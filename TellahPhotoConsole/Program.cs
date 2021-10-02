using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TellahPhotoLibrary;
using TellahPhotoLibrary.Common;

namespace TellahPhotoConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            ILogger logger = new ConsoleLogger();

            TellahPhotoApi photoApi = new TellahPhotoApi(logger);

            if (args.Length == 0)
            {
                logger.WriteLine("tellah v" + photoApi.GetVersion() + Environment.NewLine +
                    photoApi.GetUsage());
                return;
            }

            List<Task> tasks = new List<Task>();

            try
            {
                CommandLine commandLine = new CommandLine();
                commandLine.Parse(args);

                if (commandLine.HasFlag("v"))
                {
                    logger.WriteLine("tellah v" + photoApi.GetVersion());
                    return;
                }

                if (commandLine.HasValue("album-cover"))
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            photoApi.SetAlbumCover(commandLine.GetValue("album-cover"));
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorWrite(ex.Message);
                            logger.ErrorFlush();
                        }
                    }));
                }
                else if (commandLine.HasFlag("build-html") ||
                    commandLine.HasValue("build-html"))
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            photoApi.BuildHtml(commandLine);
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorWrite(ex.Message);
                            logger.ErrorFlush();
                        }
                    }));
                }
                else
                {
                    tasks.Add(Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            photoApi.ProcessAlbum(args, null);
                        }
                        catch (Exception ex)
                        {
                            logger.ErrorWrite(ex.Message);
                            logger.ErrorFlush();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                logger.ErrorWriteLine(ex.Message);
                logger.Write(photoApi.GetUsage());
                return;
            }

            // wait for processing to finish
            Task.WaitAll(tasks.ToArray());
        }
    }
}
