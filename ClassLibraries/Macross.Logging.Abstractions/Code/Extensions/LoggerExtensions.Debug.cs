﻿using System;

namespace Microsoft.Extensions.Logging
{
	public static partial class LoggerExtensions
	{
		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			string message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, 0, (object?)null, null, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			string message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, eventId, (object?)null, null, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, 0, data, null, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			object? data,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, eventId, data, null, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		public static void WriteDebug(
			this ILogger logger,
			Exception? exception)
			=> Write(logger, LogLevel.Debug, 0, (object?)null, exception, null, null);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			Exception? exception)
			=> Write(logger, LogLevel.Debug, eventId, (object?)null, exception, null, null);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, 0, (object?)null, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, eventId, (object?)null, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, 0, data, exception, message, args);

		/// <summary>
		/// Formats and writes a <see cref="LogLevel.Debug"/> log message.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> to write to.</param>
		/// <param name="eventId">The event id associated with the log.</param>
		/// <param name="data">Data to be attached to the log entry.</param>
		/// <param name="exception">The exception to log.</param>
		/// <param name="message">Format string of the log message.</param>
		/// <param name="args">An object array that contains zero or more objects to format.</param>
		public static void WriteDebug(
			this ILogger logger,
			EventId eventId,
			object? data,
			Exception? exception,
			string? message,
			params object?[]? args)
			=> Write(logger, LogLevel.Debug, eventId, data, exception, message, args);
	}
}
