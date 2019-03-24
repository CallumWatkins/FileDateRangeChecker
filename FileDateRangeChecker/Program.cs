using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;

namespace FileDateRangeChecker
{
    internal class Program
    {
        /// <summary>
        /// Entry point of the CLI.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        private static void Main(string[] args)
        {
            var cliParser = new CommandLineParser.CommandLineParser();
            var directoryArgument = new DirectoryArgument('d', "directory", "The directory containing the files to match.")
                { DirectoryMustExist = true, Optional = true, DefaultValue = new DirectoryInfo(Environment.CurrentDirectory)};
            var fileExtensionArgument = new ValueArgument<string>('x', "extension", "The file extension (without dot) to match as a regular expression pattern, e.g. \"^(pdf|jpe?g)$\" or \"db$\". Matches all extensions if omitted.")
                { Optional = true, DefaultValue = string.Empty};
            var startDateArgument = new ValueArgument<DateTime>('s', "start-date", "The range start date (YYYY-MM-DD).")
                { Optional = false };
            var endDateArgument = new ValueArgument<DateTime>('e', "end-date", "The range end date (YYYY-MM-DD).")
                { Optional = false };

            cliParser.ShowUsageHeader = "Check if a directory containing files with named date ranges is missing any dates from a given range.";
            cliParser.Arguments.Add(directoryArgument);
            cliParser.Arguments.Add(fileExtensionArgument);
            cliParser.Arguments.Add(startDateArgument);
            cliParser.Arguments.Add(endDateArgument);
            cliParser.ShowUsageOnEmptyCommandline = true;

            try
            {
                cliParser.ParseCommandLine(args);
            }
            catch (CommandLineException cle)
            {
                Console.WriteLine(cle.Message);
                cliParser.ShowUsage();
                return;
            }
            catch (DirectoryNotFoundException dnfe)
            {
                Console.WriteLine(dnfe.Message.EndsWith(" and DirectoryMustExist flag is set to true.")
                    ? dnfe.Message.Substring(0, dnfe.Message.Length - 44)
                    : dnfe.Message);
                return;
            }

            if (!cliParser.ParsingSucceeded) { return; }

            if (startDateArgument.Value > endDateArgument.Value)
            {
                Console.WriteLine("The start date of the range cannot be later than the end date.");
                return;
            }

            Console.BackgroundColor = ConsoleColor.Black;

            if (HasMissingRanges(directoryArgument.Value, fileExtensionArgument.Value, (startDateArgument.Value, endDateArgument.Value), out (DateTime, DateTime)[] missingRanges))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Missing ranges:");
                foreach ((DateTime start, DateTime end) in missingRanges)
                {
                    Console.WriteLine($"{start:yyyy-MM-dd} - {end:yyyy-MM-dd}");
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("No missing ranges.");
            }

            Console.ResetColor();
        }

        /// <summary>
        /// Check if a directory containing files with named date ranges is missing any dates from a given range.
        /// </summary>
        /// <param name="directory">The directory to scan.</param>
        /// <param name="fileExtensionPattern">A regular expression pattern for the file extension of the files to match (without the dot).</param>
        /// <param name="dateRange">The full range to check for.</param>
        /// <param name="missingRanges">Will contain any missing ranges of dates, or will be empty.</param>
        /// <returns>True if there are any missing date ranges, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="directory"/> or <paramref name="fileExtensionPattern"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the start date of the range is later than the end date.</exception>
        private static bool HasMissingRanges(DirectoryInfo directory, string fileExtensionPattern, (DateTime start, DateTime end) dateRange, out (DateTime start, DateTime end)[] missingRanges)
        {
            if (directory == null) { throw new ArgumentNullException(nameof(directory)); }
            if (fileExtensionPattern == null) { throw new ArgumentNullException(nameof(fileExtensionPattern)); }

            // File name must contain two ISO 8601 formatted dates. This regex will match the first and last dates found.
            var fileNameRegex = new Regex("(?<startDate>[0-9]{4}-[0-9]{2}-[0-9]{2}).*(?<endDate>[0-9]{4}-[0-9]{2}-[0-9]{2})");

            // File extension must match the given pattern. FileSystemInfo.Extension includes a dot, so place a dot after any start anchors.
            var fileExtensionRegex = new Regex(fileExtensionPattern.Replace("^", @"^\."));

            // Create a hashset of all dates that are covered by the files
            var fileDates = new HashSet<DateTime>(directory.EnumerateFiles()
                                                           .Where(fileInfo => fileExtensionRegex.IsMatch(fileInfo.Extension))
                                                           .Select(fileInfo => fileNameRegex.Match(fileInfo.Name))
                                                           .Where(match => match.Success)
                                                           .Select(match => (
                                                               match.Groups["startDate"].Captures[0].Value.ParseIso8601Date(),
                                                               match.Groups["endDate"].Captures[0].Value.ParseIso8601Date()
                                                           ))
                                                           .SelectMany(DateTimeExtensions.RangeToDates)
                                                           .ToList()); // Convert to list before adding to hashset to avoid rehashing

            // Find all dates in the requested range that are not in the hashset and convert them to ranges
            missingRanges = dateRange.RangeToDates()
                                     .Where(dateInRange => !fileDates.Contains(dateInRange))
                                     .DatesToRanges()
                                     .ToArray();

            return missingRanges.Length > 0;
        }
    }
}
