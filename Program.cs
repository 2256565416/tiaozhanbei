using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace CNC_dos
{
    internal class Program
    {
        private static string imagesDirectory = Directory.GetCurrentDirectory();
        private static int cncFileCounter = 0;

        private void GenerateInitialCNCFile(string fileName)
        {
            List<string> lines = new List<string>
            {
                "G21          ; 设置单位为毫米",
                "G17          ; 选择 XY 平面",
                "G90          ; 设置绝对编程模式",
                "G0 Z0        ; 提升刀具到安全高度 0mm",
                "G0 X0 Y0     ; 快速移动到原点 (0,0)",
                "G0 Z0        ; 提升刀具到安全高度",
                "G0 X0 Y0     ; 返回原点",
                "M30          ; 程序结束"
            };

            File.WriteAllLines(fileName, lines);
        }

        private void ProcessImage(string imagePath)
        {
            string outputImagePath = Path.Combine(imagesDirectory, $"image_output_{cncFileCounter}.jpg");
            string cncFileName = Path.Combine(imagesDirectory, $"cnc_{cncFileCounter}.nc");

            // 调用Python脚本处理图片
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"py.py \"{imagePath}\" \"{outputImagePath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine(result);
                    GenerateCNCFileFromOutput(result, cncFileName);
                }
            }

            cncFileCounter++;
        }

        private void GenerateCNCFileFromOutput(string output, string cncFileName)
        {
            List<string> lines = new List<string>
            {
                "G21          ; 设置单位为毫米",
                "G17          ; 选择 XY 平面",
                "G90          ; 设置绝对编程模式",
                "G0 Z0        ; 提升刀具到安全高度 0mm"
            };

            // 使用正则表达式提取坐标
            var matches = Regex.Matches(output, @"Start:\s*\((\d+),\s*(\d+)\),\s*End:\s*\((\d+),\s*(\d+)\)");
            foreach (Match match in matches)
            {
                decimal x0 = decimal.Parse(match.Groups[1].Value);
                decimal y0 = decimal.Parse(match.Groups[2].Value);
                decimal x1 = decimal.Parse(match.Groups[3].Value);
                decimal y1 = decimal.Parse(match.Groups[4].Value);

                lines.Add($"G1 X{x0} Y{y0} Z0 ; 线性插补移动到 ({x0}, {y0}) 并铣削到 Z=0mm");
                lines.Add($"G1 X{x1} Y{y1} Z-5 ; 线性插补移动到 ({x1}, {y1}) 并铣削到 Z=-5mm");
                lines.Add("G0 Z0        ; 提升刀具到安全高度");
            }
            lines.Add("M30          ; 程序结束");

            File.WriteAllLines(cncFileName, lines);
            Console.WriteLine($"CNC 文件已生成: {cncFileName}");
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.GenerateInitialCNCFile("cnc_template.nc");

            FileSystemWatcher watcher = new FileSystemWatcher
            {
                Path = imagesDirectory,
                Filter = "*.jpg",
                NotifyFilter = NotifyFilters.FileName
            };

            watcher.Created += (sender, e) =>
            {
                // 处理新文件
                program.ProcessImage(e.FullPath);

                // 等待处理完成后，暂停监视
                watcher.EnableRaisingEvents = false;

                // 等待一段时间，以确保文件处理完成
                Thread.Sleep(1000); // 可以根据需要调整等待时间

                // 重新启用监视
                watcher.EnableRaisingEvents = true;
            };

            watcher.EnableRaisingEvents = true;

            Console.WriteLine("监视目录中的新图片...");
            Console.ReadLine(); // 保持程序运行
        }
    }
}
