﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Macross.Logging.Files
{
	[ProviderAlias("Macross.Files")]
#pragma warning disable CA1812 // Remove class never instantiated
	internal class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
#pragma warning restore CA1812 // Remove class never instantiated
	{
		private static void TestDiskPermissions(string logFileDirectory, string logFileArchiveDirectory, string testFileName)
		{
			try
			{
				string LogFileFullPath = Path.Combine(logFileDirectory, testFileName);

				File.WriteAllText(LogFileFullPath, "Test");

				string LogFileArchiveFullPath = Path.Combine(logFileArchiveDirectory, testFileName);

				if (File.Exists(LogFileArchiveFullPath))
					File.Delete(LogFileArchiveFullPath);

				File.Move(LogFileFullPath, LogFileArchiveFullPath);

				File.Delete(LogFileArchiveFullPath);
			}
			catch (Exception Exception)
			{
				throw new InvalidOperationException("Disk permission test failed. Does the application have access to the paths specified by the logging configuration?", Exception);
			}
		}

		private readonly LogFileManager _LogFileManager = new LogFileManager(new FileSystem(), new SystemTime());
		private readonly ConcurrentDictionary<string, FileLogger> _Loggers = new ConcurrentDictionary<string, FileLogger>();
		private readonly ConcurrentQueue<LoggerJsonMessage> _Messages = new ConcurrentQueue<LoggerJsonMessage>();
		private readonly EventWaitHandle _StopHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
		private readonly EventWaitHandle _ArchiveNowHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
		private readonly EventWaitHandle _MessageReadyHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly Thread _LogMessageProcessingThread;
		private readonly IHostEnvironment _HostEnvironment;
		private readonly IOptionsMonitor<FileLoggerOptions> _Options;
		private readonly IDisposable _OptionsReloadToken;
		private Timer? _Timer;
		private IExternalScopeProvider? _ScopeProvider;
		private string? _LogFileNamePattern;
		private int? _LogFileMaxSizeInKilobytes;
		private JsonSerializerOptions? _JsonOptions;
		private LoggerGroupCache? _LoggerGroupCache;

		public FileLoggerProvider(IHostEnvironment hostEnvironment, IOptionsMonitor<FileLoggerOptions> options)
		{
			_HostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
			_Options = options ?? throw new ArgumentNullException(nameof(options));

			ApplyOptions(options.CurrentValue);
			_OptionsReloadToken = _Options.OnChange(ApplyOptions);

			_LogMessageProcessingThread = new Thread(LogMessageProcessingThreadBody)
			{
				Name = "Macross.Files"
			};
			_LogMessageProcessingThread.Start();
		}

		/// <summary>
		/// Finalizes an instance of the <see cref="FileLoggerProvider"/> class.
		/// </summary>
		~FileLoggerProvider()
		{
			Dispose(false);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			_StopHandle.Set();

			_LogMessageProcessingThread.Join();

			if (isDisposing)
			{
				_Timer?.Dispose();
				_LogFileManager.Dispose();
				_OptionsReloadToken.Dispose();
				_StopHandle.Dispose();
				_ArchiveNowHandle.Dispose();
				_MessageReadyHandle.Dispose();
			}
		}

		/// <inheritdoc/>
		public ILogger CreateLogger(string categoryName)
		{
			return _Loggers.GetOrAdd(
				categoryName,
				_ => new FileLogger(categoryName, AddMessage)
				{
					ScopeProvider = _ScopeProvider
				});
		}

		/// <inheritdoc/>
		public void SetScopeProvider(IExternalScopeProvider scopeProvider)
		{
			_ScopeProvider = scopeProvider;

			foreach (KeyValuePair<string, FileLogger> Logger in _Loggers)
			{
				Logger.Value.ScopeProvider = _ScopeProvider;
			}
		}

		private void ApplyOptions(FileLoggerOptions options)
		{
			string LogFileDirectory = PrepareLogFileDirectory("Log file directory", options.LogFileDirectory, FileLoggerOptions.DefaultLogFileDirectory);
			string LogFileArchiveDirectory = PrepareLogFileDirectory("Log file archive directory", options.LogFileArchiveDirectory, FileLoggerOptions.DefaultLogFileArchiveDirectory);

			string LogFileNamePattern = !string.IsNullOrWhiteSpace(options.LogFileNamePattern)
				 ? options.LogFileNamePattern.Trim()
				 : !options.IncludeGroupNameInFileName
					 ? FileLoggerOptions.DefaultLogFileNamePattern
					 : FileLoggerOptions.DefaultGroupLogFileNamePattern;

			string TestFileName = FileNameGenerator.GenerateFileName(
				_HostEnvironment.ApplicationName,
				new SystemTime(),
				"__OptionsTest__",
				LogFileNamePattern);

			if (TestFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
				throw new InvalidOperationException("Log file naming pattern cannot contain invalid characters.");

			if (options.TestDiskOnStartup)
				TestDiskPermissions(LogFileDirectory, LogFileArchiveDirectory, TestFileName + ".permtest");

			options.LogFileDirectory = LogFileDirectory;
			options.LogFileArchiveDirectory = LogFileArchiveDirectory;

			_LogFileNamePattern = LogFileNamePattern;

			_LogFileMaxSizeInKilobytes = options.LogFileMaxSizeInKilobytes > 0
				? options.LogFileMaxSizeInKilobytes
				: (int?)null;

			_JsonOptions = options.JsonOptions ?? FileLoggerOptions.DefaultJsonOptions;

			_LoggerGroupCache = new LoggerGroupCache(options.GroupOptions);

			_LogFileManager.ClearCache();
		}

		private string PrepareLogFileDirectory(string optionName, string? optionValue, string defaultValue)
		{
			if (!string.IsNullOrWhiteSpace(optionValue))
			{
				optionValue = optionValue.Trim();
				if (!optionValue.EndsWith("\\", StringComparison.OrdinalIgnoreCase))
					optionValue += '\\';
			}
			else
			{
				optionValue = defaultValue;
			}

			optionValue = FileNameGenerator.GenerateFileName(
				_HostEnvironment.ApplicationName,
				new SystemTime(),
				string.Empty,
				optionValue);

			if (optionValue.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
				throw new InvalidOperationException($"{optionName} path cannot contain invalid characters.");

			if (!Directory.Exists(optionValue))
				Directory.CreateDirectory(optionValue);

			return optionValue;
		}

		private void AddMessage(LoggerJsonMessage message)
		{
			if (string.IsNullOrWhiteSpace(message.GroupName))
				message.GroupName = _LoggerGroupCache!.ResolveGroupNameForCategoryName(message.CategoryName);

			_Messages.Enqueue(message);
			_MessageReadyHandle.Set();
		}

		private void LogMessageProcessingThreadBody(object? state)
		{
			WaitHandle[] Handles = new WaitHandle[] { _StopHandle, _ArchiveNowHandle, _MessageReadyHandle };

			while (true)
			{
				int HandleIndex = WaitHandle.WaitAny(Handles);
				if (HandleIndex == 0)
					break;
				if (HandleIndex == 1)
				{
					_Timer?.Dispose();

					FileLoggerOptions Options = _Options.CurrentValue;

					if (_Timer != null || Options.ArchiveLogFilesOnStartup)
						_LogFileManager.ArchiveLogFiles(_HostEnvironment.ApplicationName, Options, _LogFileNamePattern!);

					DateTime UtcNow = DateTime.UtcNow;
					DateTime NextArchiveTimeUtc = UtcNow.Date.AddDays(1);
					TimeSpan TimeUntilNextArchive = NextArchiveTimeUtc - UtcNow;
					if (TimeUntilNextArchive <= TimeSpan.FromMinutes(5))
						TimeUntilNextArchive = NextArchiveTimeUtc.AddDays(1) - UtcNow;

					_Timer = new Timer((s) => _ArchiveNowHandle.Set(), null, TimeUntilNextArchive, TimeSpan.FromMilliseconds(-1));
					_ArchiveNowHandle.Reset();
					continue;
				}

				DrainMessages(true);
			}

			// When exiting make sure anything remaining in the queue is pumped to files.
			DrainMessages(false);
		}

		private void DrainMessages(bool archiveLogFiles)
		{
			while (!archiveLogFiles || !_ArchiveNowHandle.WaitOne(0)) // Tight inner loop while there are messages to process.
			{
				if (!_Messages.TryDequeue(out LoggerJsonMessage Message))
					break;

				LogFile? LogFile = _LogFileManager.FindLogFile(
					_HostEnvironment.ApplicationName,
					Message.GroupName!,
					() => _Options.CurrentValue,
					_LogFileNamePattern!,
					_LogFileMaxSizeInKilobytes);

				if (LogFile != null)
				{
					try
					{
						SerializeMessageToJson(LogFile.Stream, Message).GetAwaiter().GetResult();
					}
#pragma warning disable CA1031 // Do not catch general exception types
					catch
#pragma warning restore CA1031 // Do not catch general exception types
					{
						LogFile.Toxic = true;
					}
				}
			}
		}

		private Task SerializeMessageToJson(Stream stream, LoggerJsonMessage message)
		{
			try
			{
				return JsonSerializer.SerializeAsync(stream, message, _JsonOptions);
			}
			catch (JsonException JsonException)
			{
				return JsonSerializer.SerializeAsync(
					stream,
					new LoggerJsonMessage
					{
						LogLevel = message.LogLevel,
						TimestampUtc = message.TimestampUtc,
						ThreadId = message.ThreadId,
						EventId = message.EventId,
						GroupName = message.GroupName,
						CategoryName = message.CategoryName,
						Content = $"Message with Content [{message.Content}] contained data that could not be serialized into Json.",
						Exception = LoggerJsonMessageException.FromException(JsonException)
					},
					_JsonOptions);
			}
		}
	}
}