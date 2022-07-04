using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Notus.Toolbox
{
    public static class Print
    {
        public static void Info(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }

        }
        public static void Danger(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }
        }
        public static void Warning(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            ConsoleColor currentColor = Console.ForegroundColor;
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                            Console.ForegroundColor = currentColor;
                        });
                    }
                    else
                    {
                        ConsoleColor currentColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        Console.ForegroundColor = currentColor;
                    }
                }
            }
        }
        public static void Basic(bool ShowOnScreen, string DetailsStr = "", bool PrintAsync = true)
        {
            if (ShowOnScreen == true)
            {
                if (DetailsStr == "")
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.WriteLine();
                        });
                    }
                    else
                    {
                        Console.WriteLine();
                    }
                }
                else
                {
                    if (PrintAsync == true)
                    {
                        Task.Run(() =>
                        {
                            Console.ForegroundColor = ConsoleColor.Gray;
                            Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                        });
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine(DateTime.Now.ToLongTimeString() + " -> " + DetailsStr);
                    }
                }
            }
        }
    }
}
