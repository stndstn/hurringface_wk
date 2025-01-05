using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Tesseract;

namespace ScanID
{
    public class ScanIDOCR
    {
        static string EncodeImageFileToBase64(string imageFileName)
        {
            if (!System.IO.File.Exists(imageFileName))
            {
                Console.WriteLine("File not found: " + imageFileName);
                throw new Exception("File not found: " + imageFileName);
            }

            string b64Image = "";
            using (var stream = System.IO.File.OpenRead(imageFileName))
            {
                byte[] b = new byte[stream.Length];
                stream.Read(b, 0, b.Length);
                b64Image = Convert.ToBase64String(b);
            }

            if (b64Image.Length == 0)
            {
                Console.WriteLine("File is empty: " + imageFileName);
                throw new Exception("File is empty: " + imageFileName);
            }

            return b64Image;
        }

        public static ScanMyKadResult ScanMyKad(string baseAddrUrl, string imageFileName)
        {
            Console.WriteLine($"ScanMyKad imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);

            DateTime dtStart = DateTime.Now;
            Console.WriteLine($"PostOCRWithRegionRequest start...");
            List<Line> lines = PostOCRWithRegionRequest(baseAddrUrl, b64Image);
            DateTime dtEnd = DateTime.Now;
            Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

            // remove </s> from the start of 1st line
            if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
            {
                lines[0].Text = lines[0].Text.Replace("</s>", "");
            }

            foreach (Line line in lines)
            {
                Console.WriteLine(line.ExtToString());
            }
            /*
            Console.WriteLine("==== Merged lines ====");
            IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
            foreach (Line line in linesMerged)
            {
                Console.WriteLine(line.ExtToString());
            }
            */
            Console.WriteLine("======================");
#if true
            using (SKImage bmpImage = SKImage.FromEncodedData(imageFileName))
            {
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lines)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0] - 1, (int)line.BoundingBox[1] - 1, (int)line.BoundingBox[2] + 1, (int)line.BoundingBox[3] + 1);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImage.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(lineTess.Text))
                                continue;

                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(tessBlocks[0].Text)
                                && tessBlocks[0].Confidence != null
                                && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.5)
                                {
                                    line.Text = tessBlocks[0].Text.Trim();
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> {
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[0],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[1],
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[2],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[3]
                                    };
                                }
                                Console.WriteLine(line.ExtToString());
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
                    foreach (Line line in linesMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    DateTime dtStart2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad start...");
                    ScanMyKadResult scanMyKadResult = ExtractFieldsFromReadResultOfMyKad(linesMerged);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanMyKadResult;
                }
            }
#endif
            {
                DateTime dtStart2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad start...");
                ScanMyKadResult scanMyKadResult = ExtractFieldsFromReadResultOfMyKad(lines);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfMyKad ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                return scanMyKadResult;

            }
        }

        public static ScanMYDLResult ScanMYDL(string baseAddrUrl, string imageFileName)
        {
            Console.WriteLine($"ScanMYDL imageFileName: {imageFileName}");

            string b64Image = EncodeImageFileToBase64(imageFileName);

            int width = 0, height = 0;
            using (SKImage bmpImage = SKImage.FromEncodedData(imageFileName))
            {
                width = bmpImage.Width;
                height = bmpImage.Height;
                if (width == 0 || height == 0)
                {
                    Console.WriteLine("File is empty: " + imageFileName);
                    throw new Exception("File is empty: " + imageFileName);
                }

                DateTime dtStart = DateTime.Now;
                Console.WriteLine($"PostOCRWithRegionRequest start...");
                List<Line> lines = PostOCRWithRegionRequest(baseAddrUrl, b64Image);
                DateTime dtEnd = DateTime.Now;
                Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // remove </s> from the start of 1st line
                if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
                {
                    lines[0].Text = lines[0].Text.Replace("</s>", "");
                }

                foreach (Line line in lines)
                {
                    Console.WriteLine(line.ExtToString());
                }

#if true
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lines)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0] - 1, (int)line.BoundingBox[1] - 1, (int)line.BoundingBox[2] + 1, (int)line.BoundingBox[3] + 1);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImage.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            if(string.IsNullOrWhiteSpace(lineTess.Text))
                                continue;

                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(tessBlocks[0].Text) 
                                && tessBlocks[0].Confidence != null 
                                && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.5)
                                {
                                    line.Text = tessBlocks[0].Text.Trim();
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> { 
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[0], 
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[1], 
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[2], 
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[3]
                                    };
                                }
                                Console.WriteLine(line.ExtToString());
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
                    foreach (Line line in linesMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    DateTime dtStart2 = DateTime.Now;
                    ScanMYDLResult scanMYDLResult = ExtractFieldsFromReadResultOfMYDL(linesMerged, width);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfMYDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanMYDLResult;
                }
#endif
                {
                    DateTime dtStart2 = DateTime.Now;
                    ScanMYDLResult scanMYDLResult = ExtractFieldsFromReadResultOfMYDL(lines, width);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfMYDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanMYDLResult;
                }
            }
        }

        public static ScanPHUMIDResult ScanPHUMID(string baseAddrUrl, string imageFileName)
        {
            Console.WriteLine($"ScanPHUMID imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);

            DateTime dtStart = DateTime.Now;
            Console.WriteLine($"PostOCRWithRegionRequest start...");
            List<Line> lines = PostOCRWithRegionRequest(baseAddrUrl, b64Image);
            DateTime dtEnd = DateTime.Now;
            Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

            // remove </s> from the start of 1st line
            if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
            {
                lines[0].Text = lines[0].Text.Replace("</s>", "");
            }

            foreach (Line line in lines)
            {
                Console.WriteLine(line.ExtToString());
            }
            /*
            Console.WriteLine("==== Merged lines ====");
            IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
            foreach (Line line in linesMerged)
            {
                Console.WriteLine(line.ExtToString());
            }
            */
            Console.WriteLine("======================");
#if true
            using (SKImage bmpImage = SKImage.FromEncodedData(imageFileName))
            {
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lines)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0] - 1, (int)line.BoundingBox[1] - 1, (int)line.BoundingBox[2] + 1, (int)line.BoundingBox[3] + 1);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImage.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(lineTess.Text))
                                continue;

                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(tessBlocks[0].Text)
                                && tessBlocks[0].Confidence != null
                                && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.5)
                                {
                                    line.Text = tessBlocks[0].Text.Trim();
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> {
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[0],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[1],
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[2],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[3]
                                    };
                                }
                                Console.WriteLine(line.ExtToString());
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
                    foreach (Line line in linesMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    DateTime dtStart2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID start...");
                    ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(linesMerged);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanPHUMIDResult;
                }
            }
#endif
            {
                DateTime dtStart2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID start...");
                ScanPHUMIDResult scanPHUMIDResult = ExtractFieldsFromReadResultOfPHUMID(lines);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHUMID ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                return scanPHUMIDResult;
            }
        }

        public static ScanPHDLResult ScanPHDL(string baseAddrUrl, string imageFileName)
        {
            Console.WriteLine($"ScanPHDL imageFileName: {imageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);

            DateTime dtStart = DateTime.Now;
            Console.WriteLine($"PostOCRWithRegionRequest start...");
            List<Line> lines = PostOCRWithRegionRequest(baseAddrUrl, b64Image);
            DateTime dtEnd = DateTime.Now;
            Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

            // remove </s> from the start of 1st line
            if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
            {
                lines[0].Text = lines[0].Text.Replace("</s>", "");
            }

            foreach (Line line in lines)
            {
                Console.WriteLine(line.ExtToString());
            }
            /*
            Console.WriteLine("==== Merged lines ====");
            IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
            foreach (Line line in linesMerged)
            {
                Console.WriteLine(line.ExtToString());
            }
            Console.WriteLine("======================");
            */
#if true
            using (SKImage bmpImage = SKImage.FromEncodedData(imageFileName))
            {
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lines)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0] - 1, (int)line.BoundingBox[1] - 1, (int)line.BoundingBox[2] + 1, (int)line.BoundingBox[3] + 1);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImage.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(lineTess.Text))
                                continue;

                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(tessBlocks[0].Text)
                                && tessBlocks[0].Confidence != null
                                && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.5)
                                {
                                    line.Text = tessBlocks[0].Text.Trim();
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> {
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[0],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[1],
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[2],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[3]
                                    };
                                }
                                Console.WriteLine(line.ExtToString());
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
                    foreach (Line line in linesMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    DateTime dtStart2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL start...");
                    ScanPHDLResult scanPHDLResult = ExtractFieldsFromReadResultOfPHDL(linesMerged);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanPHDLResult;
                }
            }
#endif
            {
                DateTime dtStart2 = DateTime.Now;
                ScanPHDLResult scanPHDLResult = ExtractFieldsFromReadResultOfPHDL(lines);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHDL ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                return scanPHDLResult;
            }
        }

        public static ScanPHNIResult ScanPHNI(string baseAddrUrl, string imageFileName, string backImageFileName)
        {
            Console.WriteLine($"ScanPHNI imageFileName: {imageFileName}");
            Console.WriteLine($"ScanPHNI backImageFileName: {backImageFileName}");
            string b64Image = EncodeImageFileToBase64(imageFileName);
            string b64ImageBack = "";
            if (string.IsNullOrEmpty(backImageFileName) == false)
            {
                b64ImageBack = EncodeImageFileToBase64(backImageFileName);
            }
            ScanPHNIResult scanPHNIResult = null;
            List<Line> lines = null;
            {
                DateTime dtStart = DateTime.Now;
                Console.WriteLine($"ScanPHNI front...");
                Console.WriteLine($"PostOCRWithRegionRequest start...");
                lines = PostOCRWithRegionRequest(baseAddrUrl, b64Image);
                DateTime dtEnd = DateTime.Now;
                Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");
            }

            // remove </s> from the start of 1st line
            if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
            {
                lines[0].Text = lines[0].Text.Replace("</s>", "");
            }

            foreach (Line line in lines)
            {
                Console.WriteLine(line.ExtToString());
            }
            /*
            Console.WriteLine("==== Merged lines ====");
            IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
            foreach (Line line in linesMerged)
            {
                Console.WriteLine(line.ExtToString());
            }
            Console.WriteLine("======================");
            */
#if true
            using (SKImage bmpImage = SKImage.FromEncodedData(imageFileName))
            {
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lines)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0] - 1, (int)line.BoundingBox[1] - 1, (int)line.BoundingBox[2] + 1, (int)line.BoundingBox[3] + 1);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImage.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            if (string.IsNullOrWhiteSpace(lineTess.Text))
                                continue;

                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (!string.IsNullOrWhiteSpace(tessBlocks[0].Text)
                                && tessBlocks[0].Confidence != null
                                && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.5)
                                {
                                    line.Text = tessBlocks[0].Text.Trim();
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> {
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[0],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[1],
                                        line.BoundingBox[0] + tessBlocks[0].Baseline[2],
                                        line.BoundingBox[1] + tessBlocks[0].Baseline[3]
                                    };
                                }
                                Console.WriteLine(line.ExtToString());
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");

                    Console.WriteLine("==== Merged lines ====");
                    IList<Line> linesMerged = MergeLinesInSameYPosIntoOneLine(lines);
                    foreach (Line line in linesMerged)
                    {
                        Console.WriteLine(line.ExtToString());
                    }
                    Console.WriteLine("======================");

                    DateTime dtStart2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI start...");
                    scanPHNIResult = ExtractFieldsFromReadResultOfPHNI(linesMerged);
                    DateTime dtEnd2 = DateTime.Now;
                    Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                    return scanPHNIResult;
                }
            }
#endif
            {
                DateTime dtStart2 = DateTime.Now;
                scanPHNIResult = ScanIDOCR.ExtractFieldsFromReadResultOfPHNI(lines);
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
            }

            if (string.IsNullOrEmpty(backImageFileName) || string.IsNullOrEmpty(b64ImageBack))
            {
                return scanPHNIResult;
            }

            Console.WriteLine($"PostOCRWithRegionRequest start...");
            using (SKImage bmpImageBak = SKImage.FromEncodedData(backImageFileName))
            {
                Console.WriteLine($"ScanPHNI back...");
                DateTime dtStart = DateTime.Now;
                Console.WriteLine($"PostOCRWithRegionRequest start...");
                List<Line> linesBack = PostOCRWithRegionRequest(baseAddrUrl, b64ImageBack);
                DateTime dtEnd = DateTime.Now;
                Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // remove </s> from the start of 1st line
                if (lines.Count > 0 && lines[0].Text.StartsWith("</s>"))
                {
                    lines[0].Text = lines[0].Text.Replace("</s>", "");
                }

                foreach (Line line in linesBack)
                {
                    Console.WriteLine(line.ExtToString());
                }
                Console.WriteLine("==== Merged lines ====");
                IList<Line> linesBackMerged = MergeLinesInSameYPosIntoOneLine(linesBack);
                foreach (Line line in linesBackMerged)
                {
                    Console.WriteLine(line.ExtToString());
                }
                Console.WriteLine("======================");
#if true
                if (bmpImageBak != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in linesBackMerged)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[2], (int)line.BoundingBox[3]);
                            imageLine = bmpImageBak.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            //rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            //imageLine = bmpImageBak.Subset(rect);
                            continue;   // no need to scan with tesseract for Florence-base 
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var tessBlocks = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in tessBlocks)
                        {
                            linesTess.Add(lineTess);
                        }

                        if (tessBlocks.Count == 1)
                        {
                            if (tessBlocks[0].Confidence != null && tessBlocks[0].Confidence.Value > 0.3)
                            {
                                // take boundingBox and baseline from Tesseract to accurate line height.
                                // but take scanned text only if confidence > 0.9

                                if (tessBlocks[0].Confidence.Value > 0.9)
                                {
                                    line.Text = tessBlocks[0].Text;
                                    line.Confidence = tessBlocks[0].Confidence;
                                }
                                if (line.BoundingBox.Count == 4)
                                {
                                    // update bounding box
                                    line.BoundingBox[0] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[0];
                                    line.BoundingBox[1] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[1];
                                    line.BoundingBox[2] = line.BoundingBox[0] + tessBlocks[0].BoundingBox[2];
                                    line.BoundingBox[3] = line.BoundingBox[1] + tessBlocks[0].BoundingBox[3];
                                    line.Baseline = new List<double?> { line.BoundingBox[0] + tessBlocks[0].Baseline[0], line.BoundingBox[1] + tessBlocks[0].Baseline[1] };
                                }
                            }
                        }
                    }
                    Console.WriteLine("==== Lines read by Tesseract ====");
                    foreach (Line lineTess in linesTess)
                    {
                        Console.WriteLine(lineTess.ExtToString());
                    }
                    Console.WriteLine("======================");
                }
#endif
                DateTime dtStart2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNIBK start...");
                ScanPHNIBKResult scanPHNIBKResult = ScanIDOCR.ExtractFieldsFromReadResultOfPHNIBK(lines, SKBitmap.FromImage(bmpImageBak));
                DateTime dtEnd2 = DateTime.Now;
                Console.WriteLine($"ExtractFieldsFromReadResultOfPHNI ({(dtEnd2 - dtStart2).TotalSeconds} sec)\n");
                if (scanPHNIBKResult.IsQRCodeDataValid)
                {
                    scanPHNIResult.documentIssueDate = scanPHNIBKResult.QRCode_DateIssued;
                    scanPHNIResult.lastNameOrFullName = scanPHNIBKResult.QRCode_subject_lName;
                    scanPHNIResult.firstName = scanPHNIBKResult.QRCode_subject_fName;
                    scanPHNIResult.middleName = scanPHNIBKResult.QRCode_subject_mName;
                    scanPHNIResult.gender = EncodeGender(scanPHNIBKResult.QRCode_subject_sex);
                    scanPHNIResult.dateOfBirth = scanPHNIBKResult.QRCode_subject_DOB;
                    scanPHNIResult.placeOfBirth = scanPHNIBKResult.QRCode_subject_POB;
                    scanPHNIResult.documentNumber = scanPHNIBKResult.QRCode_subject_PCN;
                }
                return scanPHNIResult;
            }
        }

        static string EncodeGender(string gender)
        {
            if (string.IsNullOrEmpty(gender))
                return "U";

            switch (gender.ToUpper())
            {
                case "MALE":
                    return "M";
                case "FEMALE":
                    return "F";
                default:
                    return gender;
            }
        }

#if false
        public static List<Line> PostOCRWithRegionRequest(string baseAddrUrl, string b64Image)
        {
            string ret = "";
            List<Line> lines = new List<Line>();
            // Create a new instance of the HttpClient class
            using (var client = new HttpClient())
            {
                // Create a new instance of the MyRequest class
                var jsonReq = new JObject();
                jsonReq["b64"] = b64Image;

                // serialize jsonReq
                var strJsonReq = jsonReq.ToString();

                // Create a new instance of the StringContent class
                var content = new StringContent(strJsonReq, Encoding.UTF8, "application/json");

                // Post the request to the web service
                //var response = client.PostAsync($"{BASEADDR_URL}ocrWithRegionB64", content).GetAwaiter().GetResult();
                DateTime dtStart = DateTime.Now;
                var response = client.PostAsync($"{baseAddrUrl}ocrWithRegion", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;

                Console.WriteLine($"PostOCRWithRegionRequest ({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OCR_WITH_REGION>"))
                    {
                        ret = jsonRes["<OCR_WITH_REGION>"].ToString();
                        //Console.WriteLine("<OCR_WITH_REGION>:" + ret);
                        JObject jsonRet = JObject.Parse(ret);
                        JArray labels = (JArray)jsonRet["labels"];
                        JArray boxes = (JArray)jsonRet["quad_boxes"];
                        for (int i = 0; i < labels.Count; i++)
                        {
                            Console.WriteLine(labels[i]);
                            Console.WriteLine(boxes[i]);
                            Line line = new Line();
                            line.Text = labels[i].ToString();
                            JArray jsonBoundingBox = (JArray)boxes[i];
                            List<double?> boundingBox = new List<double?>();
                            for (int j = 0; j < jsonBoundingBox.Count; j++)
                            {
                                boundingBox.Add((double)jsonBoundingBox[j]);
                            }
                            line.BoundingBox = boundingBox;
                            lines.Add(line);
                        }
                    }
                    else
                    {
                        Console.WriteLine(responseBody);
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    throw new Exception("Error: " + response.StatusCode);
                }
            }
            return lines;
        }
#else
        public static List<Line> PostOCRWithRegionRequest(string baseAddrUrl, string b64Image)
        {
            string ret = "";
            List<Line> lines = new List<Line>();
            // Create a new instance of the HttpClient class
            using (var client = new HttpClient())
            {
                // Create a new instance of the MyRequest class
                var jsonReq = new JObject();
                jsonReq["b64"] = b64Image;

                // serialize jsonReq
                var strJsonReq = jsonReq.ToString();

                // Create a new instance of the StringContent class
                var content = new StringContent(strJsonReq, Encoding.UTF8, "application/json");

                // Post the request to the web service
                //var response = client.PostAsync($"{BASEADDR_URL}ocrWithRegionB64", content).GetAwaiter().GetResult();
                DateTime dtStart = DateTime.Now;
                Console.WriteLine($"PostOCRWithRegionRequest PostAsync start...");
                var response = client.PostAsync($"{baseAddrUrl}ocrWithRegion", content).GetAwaiter().GetResult();
                DateTime dtEnd = DateTime.Now;

                Console.WriteLine($"PostOCRWithRegionRequest PostAsync ({(dtEnd - dtStart).TotalSeconds} sec)\n");

                // Check the status code of the response
                if (response.IsSuccessStatusCode)
                {
                    // Get the response content
                    var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ==== responseBody ====\n {responseBody}\n==========");

                    // Deserialize the response content to a string
                    var jsonRes = JObject.Parse(responseBody);
                    if (jsonRes.ContainsKey("<OCR_WITH_REGION>"))
                    {
                        ret = jsonRes["<OCR_WITH_REGION>"].ToString();
                        //Console.WriteLine("<OCR_WITH_REGION>:" + ret);
                        JObject jsonRet = JObject.Parse(ret);
                        JArray labels = (JArray)jsonRet["labels"];
                        JArray boxes = (JArray)jsonRet["quad_boxes"];
                        for (int i = 0; i < labels.Count; i++)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] labels[{i}]: {labels[i]}");
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] boxes[{i}]: {boxes[i]}");
                            Line line = new Line();
                            line.Text = labels[i].ToString().Trim();
                            if (string.IsNullOrEmpty(line.Text))
                            {
                                continue;
                            }
                            JArray jsonBoundingBox = (JArray)boxes[i];
                            List<double?> boundingBox = new List<double?>();
                            for (int j = 0; j < jsonBoundingBox.Count; j++)
                            {
                                boundingBox.Add((double)jsonBoundingBox[j]);
                            }
                            line.BoundingBox = boundingBox;
                            if(boundingBox.Count == 8)
                            {
                                List<double?> baseline = new List<double?>();
                                baseline.Add(boundingBox[6]); // X1 Left Bottom X
                                baseline.Add(boundingBox[7]); // Y1 Left Bottom Y
                                baseline.Add(boundingBox[4]); // X2 Right Bottom X
                                baseline.Add(boundingBox[5]); // Y2 Right Bottom Y
                            }
                            lines.Add(line);
                        }
                    }
                    else
                    {
                        Console.WriteLine(responseBody);
                    }
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    throw new Exception("Error: " + response.StatusCode);
                }
            }
            return lines;
        }
#endif
        public static ScanMYDLResult ExtractFieldsFromReadResultOfMYDL(IList<Line> linesAll, int widthImageOriginal/*, SkiaSharp.SKImage bmpImage = null*/)
        {
            try
            {
                const float FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM = 0.7f;
                LabelInfo labelLESEN_MEMANDU = new LabelInfo("LESEN MEMANDU");
                LabelInfo labelMALAYSIA = new LabelInfo("MALAYSIA");
                LabelInfo labelDRIVING_LICENCE_MALAYSIA = new LabelInfo("DRIVING LICENCE MALAYSIA");
                LabelInfo labelDRIVING_LICENCE = new LabelInfo("DRIVING LICENCE");
                LabelInfo labelDRIVING = new LabelInfo("DRIVING");
                LabelInfo labelLICENCE = new LabelInfo("LICENCE");
                LabelInfo labelWarganegara_Nationality = new LabelInfo("Warganegara / Nationality");
                LabelInfo labelNo_Pengenalan_Identity_No = new LabelInfo("No. Pengenalan / Identity No.");
                LabelInfo labelKelas_Class = new LabelInfo("Kelas / Class");
                LabelInfo labelTempoh_Validity = new LabelInfo("Tempoh / Validity");
                LabelInfo labelAlamat_Address = new LabelInfo("Alamat / Address");

                ScanMYDLResult result = new ScanMYDLResult();

                string IDNUM = "";
                string NATIONALITY = "";
                string NAME = "";
                string CLASS = "";
                string VALID_FROM = "";
                string VALID_UNTIL = "";
                string ADDRESS1 = "";
                string ADDRESS2 = "";
                string ADDRESS3 = "";
                string POSTCODE = "";
                string CITY = "";
                string STATE = "";

                //Regex regexValidFromValidUntil = new Regex(@"\d{1,2}/\d{1,2}/\d{4} - \d{1,2}/\d{1,2}/\d{4}");
                Regex regexValidFromValidUntil = new Regex(@"\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}[\s\-|]*\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}");
                Regex regexValidDate = new Regex(@"\d{1,2}[\s\/]+\d{1,2}[\s\/]+\d{4}");
                Regex regexNationality = new Regex(@"^[a-zA-Z]{3}$|^MALAYSIA$");
                Regex regexFiveDigitsNumber = new Regex(@"^\d{5}$");
                char[] separatorBlank = { ' ' };
                double? bottomOfHeaderArea = null;

                List<Line> linesField = new List<Line>();   // lines valid and not label
                                                            //List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label
                                                            //IList<Line> linesInTheSameLine = MergeLinesInSameYPosIntoOneLine(linesAll);
                foreach (Line line in linesAll)
                {
                    string text = line.Text.Trim();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()}");

                    double? angle = line.ExtGetAngle();
                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        continue;
                    }

                    if (!labelLESEN_MEMANDU.IsLabelFound)
                    {
                        Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                        if (labelLESEN_MEMANDU.MatchTitleExactly(lineUpper))
                            continue;
                    }
                    if (!labelNo_Pengenalan_Identity_No.IsLabelFound)
                    {
                        if (labelNo_Pengenalan_Identity_No.MatchTitleExactly(line))
                            continue;
                    }
                    if (!labelDRIVING_LICENCE_MALAYSIA.IsLabelFound 
                        && !labelDRIVING_LICENCE.IsLabelFound
                        && !labelLICENCE.IsLabelFound
                        && !labelDRIVING.IsLabelFound 
                        )
                    {
                        Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                        if (labelDRIVING_LICENCE_MALAYSIA.MatchTitleExactly(lineUpper))
                        {
                            if (bottomOfHeaderArea == null || bottomOfHeaderArea < labelDRIVING_LICENCE_MALAYSIA.Bottom)
                                bottomOfHeaderArea = labelDRIVING_LICENCE_MALAYSIA.Bottom;
                            continue;
                        }
                    }
                    if (!labelDRIVING_LICENCE_MALAYSIA.IsLabelFound
                        && !labelDRIVING_LICENCE.IsLabelFound
                        && !labelLICENCE.IsLabelFound
                        && !labelDRIVING.IsLabelFound
                        )
                    {
                        Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                        if (labelDRIVING_LICENCE.MatchTitleExactly(lineUpper))
                        {
                            if (bottomOfHeaderArea == null || bottomOfHeaderArea < labelDRIVING_LICENCE.Bottom)
                                bottomOfHeaderArea = labelDRIVING_LICENCE.Bottom;
                            continue;
                        }
                    }
                    if (!labelDRIVING_LICENCE_MALAYSIA.IsLabelFound
                        && !labelDRIVING_LICENCE.IsLabelFound
                        && !labelDRIVING.IsLabelFound
                        )
                    {
                        Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                        if (labelDRIVING.MatchTitleExactly(lineUpper))
                        {
                            if (bottomOfHeaderArea == null || bottomOfHeaderArea < labelDRIVING.Bottom)
                                bottomOfHeaderArea = labelDRIVING.Bottom;
                            continue;
                        }
                    }
                    if (!labelDRIVING_LICENCE_MALAYSIA.IsLabelFound
                        && !labelDRIVING_LICENCE.IsLabelFound
                        && !labelLICENCE.IsLabelFound
                        )
                    {
                        Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                        if (labelLICENCE.MatchTitleExactly(lineUpper))
                        {
                            if (bottomOfHeaderArea == null || bottomOfHeaderArea < labelLICENCE.Bottom)
                                bottomOfHeaderArea = labelLICENCE.Bottom;
                            continue;
                        }
                    }
                    if (!labelWarganegara_Nationality.IsLabelFound)
                    {
                        if (labelWarganegara_Nationality.MatchTitleExactly(line))
                            continue;
                    }
                    if (!labelDRIVING_LICENCE_MALAYSIA.IsLabelFound && !labelMALAYSIA.IsLabelFound)
                    {
                        if (bottomOfHeaderArea == null || line.ExtGetTop() < bottomOfHeaderArea)
                        {
                            Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Replace(" ", "").Trim());
                            if (labelMALAYSIA.MatchTitleExactly(lineUpper))
                            {
                                // take this line only if DRIVING_LICENCE is not found
                                if (bottomOfHeaderArea == null && labelDRIVING_LICENCE.IsLabelFound == false && labelDRIVING.IsLabelFound == false && labelLICENCE.IsLabelFound == false)
                                    bottomOfHeaderArea = labelMALAYSIA.Bottom;
                                continue;
                            }
                        }
                    }
                    if (!labelKelas_Class.IsLabelFound)
                    {
                        if (labelKelas_Class.MatchTitleExactly(line))
                            continue;
                    }
                    if (!labelTempoh_Validity.IsLabelFound)
                    {
                        if (labelTempoh_Validity.MatchTitleExactly(line))
                            continue;
                    }
                    if (!labelAlamat_Address.IsLabelFound)
                    {
                        if (labelAlamat_Address.MatchTitleExactly(line))
                            continue;
                    }
                    if (bottomOfHeaderArea == null || line.ExtGetTop() > bottomOfHeaderArea)
                    {
                        linesField.Add(line);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] line: {line.Text} --> ignored because above the herder area");
                    }

                }// foreach lines in other columns

                if (labelDRIVING_LICENCE_MALAYSIA.IsLabelFound == false 
                    && labelDRIVING_LICENCE.IsLabelFound == false 
                    && labelDRIVING.IsLabelFound == true 
                    && labelLICENCE.IsLabelFound == true)
                {
                    Line lineUpper = labelDRIVING.LineMacthed.MergedLine(labelLICENCE.LineMacthed);
                    bool bMatchMergedLine = labelDRIVING_LICENCE.MatchTitleExactly(lineUpper);
                    System.Diagnostics.Debug.WriteLine($"MergedLine {lineUpper} --> DRIVING_LICENCE");
                }

                if (bottomOfHeaderArea != null)
                {
                    List<Line> linesTemp = new List<Line>();
                    foreach (Line line in linesField)
                    {
                        if (line.ExtGetTop() > bottomOfHeaderArea)
                            linesTemp.Add(line);
                    }
                    linesField = linesTemp;
                }



                int countLinesField = linesField.Count;
                if (countLinesField > 0)
                {
                    // classify fields into main column and other (right aligned) columns
                    // main column contains: NAME, NATIONALITY, CLASS, VALID_FROM, VALID_UNTIL, ADDRESS1, ADDRESS2, ADDRESS3, POSTCODE, CITY, STATE
                    // other columns contains: IDNUM

                    int idxMedianLinesField = countLinesField / 2;
                    var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
                    double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).ExtGetLeft();
                    //double? leftMiddleOfFields = leftMedian;
                    /*
                    if (labelLESEN_MEMANDU.IsLabelFound && labelMALAYSIA.IsLabelFound)
                    {
                        double? middleOfFields = (labelLESEN_MEMANDU.Right + labelMALAYSIA.Left) / 2;
                        if (middleOfFields.HasValue)
                            leftMiddleOfFields = middleOfFields;
                    }
                    else if (labelDRIVING_LICENCE_MALAYSIA.IsLabelFound)
                    {
                        double? middleOfFields = (labelDRIVING_LICENCE_MALAYSIA.Right + labelDRIVING_LICENCE_MALAYSIA.Left) / 2;
                        if (middleOfFields.HasValue)
                            leftMiddleOfFields = middleOfFields;
                    }
                    else if (labelDRIVING_LICENCE.IsLabelFound && labelMALAYSIA.IsLabelFound)
                    {
                        double? middleOfFields = (labelDRIVING_LICENCE.Right + labelMALAYSIA.Left) / 2;
                        if (middleOfFields.HasValue)
                            leftMiddleOfFields = middleOfFields;
                    }
                    else if (labelLESEN_MEMANDU.IsLabelFound)
                    {
                        double? middleOfFields = labelLESEN_MEMANDU.Right / 2;
                        if (middleOfFields.HasValue)
                            leftMiddleOfFields = middleOfFields;
                    }
                    else if (labelDRIVING_LICENCE.IsLabelFound)
                    {
                        double? middleOfFields = labelDRIVING_LICENCE.Right + labelMALAYSIA.Left;
                        if (middleOfFields.HasValue)
                            leftMiddleOfFields = middleOfFields;
                    }
                    */
                    double? heightName = null;
                    double? bottomName = null;
                    double heightFilter = 0f;
                    // sort from top to bottom
                    //linesInMainColumn.OrderBy(l => l.ExtGetTop());
                    linesField.OrderBy(l => l.ExtGetTop());

                    IList<Line> linesFieldMerged = MergeLinesInSameYPosIntoOneLine(linesField.ToList());
                    List<Line> linesFieldUnderNameMerged = new List<Line>();
                    // find NAME 
                    foreach (Line line in linesFieldMerged)
                    {
                        if (string.IsNullOrEmpty(NAME))
                        {
                            // NAME is under DRIVING_LICENCE
                            if ((labelDRIVING_LICENCE_MALAYSIA.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelDRIVING_LICENCE_MALAYSIA.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelDRIVING_LICENCE_MALAYSIA.Bottom) < labelDRIVING_LICENCE_MALAYSIA.Height * 4
                                /*&& labelDRIVING_LICENCE.Height < line.ExtGetHeight()*/)
                                )
                            || (labelDRIVING_LICENCE.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelDRIVING_LICENCE.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelDRIVING_LICENCE.Bottom) < labelDRIVING_LICENCE.Height * 4
                                /*&& labelDRIVING_LICENCE.Height < line.ExtGetHeight()*/)
                                )
                            || (labelDRIVING.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelDRIVING.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelDRIVING.Bottom) < labelDRIVING.Height * 4
                                /*&& labelDRIVING.Height < line.ExtGetHeight()*/)
                                )
                            || (labelLICENCE.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelLICENCE.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelLICENCE.Bottom) < labelLICENCE.Height * 6
                                /*&& labelLICENCE.Height < line.ExtGetHeight()*/)
                                )
                            || (labelMALAYSIA.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelMALAYSIA.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelMALAYSIA.Bottom) < labelMALAYSIA.Height * 3
                                ))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NAME");
                                NAME = line.Text;
                                heightName = line.ExtGetHeight();
                                bottomName = line.ExtGetBottom();
                                heightFilter = (double)(heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM);
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] heightFilter = {heightFilter}");
                                continue;
                            }
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN but expect NAME here");
                        }

                        if (bottomName != null && line.ExtGetTop() > bottomName)
                        {
                            if(line.ExtGetHeight() > heightFilter)
                            {
                                linesFieldUnderNameMerged.Add(line);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN field or label smaller than fields expected.");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN field not under name field");
                        }
                    }
                    /*
                    List<Line> linesUnderName = new List<Line>();
                    // filter lines above NAME
                    foreach (Line line in linesFieldUnderNameMerged)
                    {
                        if (bottomName != null && line.ExtGetTop() > bottomName)
                        {
                            linesUnderName.Add(line);
                        }
                    }
                    */
#if false
                    if (bmpImage != null)
                    {
                        List<Line> linesTess = new List<Line>();
                        foreach (Line line in linesUnderName)
                        {
                            SkiaSharp.SKImage imageLine = null;
                            SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                            if (line.BoundingBox.Count == 4)
                            {
                                rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[2], (int)line.BoundingBox[3]);
                                imageLine = bmpImage.Subset(rect);
                            }
                            else if (line.BoundingBox.Count == 8)
                            {
                                rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                                imageLine = bmpImage.Subset(rect);
                            }

                            if (rect.IsEmpty)
                                continue;

                            if (imageLine == null)
                                continue;

                            SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            var lines = OCRLinesWithTesseractEncodedData(skData.ToArray());
                            foreach (Line lineTess in lines)
                            {
                                linesTess.Add(lineTess);
                            }
                        }
                        linesUnderName = linesTess;
                    }
#endif
                    double? leftEdgeOfBlock = linesFieldUnderNameMerged.Min(l => l.ExtGetLeft());
                    double? rightEdgeOfBlock = linesFieldUnderNameMerged.Max(l => l.ExtGetRight());
                    double? topEdgeOfBlock = linesFieldUnderNameMerged.Min(l => l.ExtGetTop());
                    double? bottomEdgeOfBlock = linesFieldUnderNameMerged.Max(l => l.ExtGetBottom());
                    double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.ExtGetLeft());
                    double? avgLeft = sumLeft / 5;
                    double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
                    double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
                    double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
                    double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

                    /*
                    //var linesInMainColumn = linesUnderName.Where(l => (decimal)l.ExtGetLeft() <= (decimal)leftMiddleOfFields);
                    var linesOutOfMainColumn = linesFieldUnderNameMerged.Where(l => (decimal)l.ExtGetLeft() > (decimal)leftMiddleOfFields);
                    linesOutOfMainColumn = MergeLinesInSameYPosIntoOneLine(linesOutOfMainColumn.ToList());

                    // find IDNUM
                    Line lineIDNum = null;
                    foreach (var line in linesOutOfMainColumn)
                    {
                        if (heightName.HasValue)
                        {
                            if (line.ExtGetHeight() < heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * {FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} = {heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} --> ignored");
                                continue;
                            }
                        }
                        if (string.IsNullOrEmpty(IDNUM))
                        {
                            // IDNUM is under NAME
                            if (!string.IsNullOrEmpty(NAME) &&
                                ((double)(line.ExtGetBottom() - bottomName) > 0
                                && (double)(line.ExtGetBottom() - bottomName) < heightName * 4
                                && Math.Abs((decimal)heightName - (decimal)line.ExtGetHeight()) < (decimal)(heightName / 2))
                                )
                            {
                                // No_Pengenalan_Identity_No
                                string idnum = line.Text.Trim().Replace(" ", "");
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {idnum} --> IDNUM");
                                IDNUM = idnum;
                                lineIDNum = line;
                                break;
                            }
                        }
                    }
                    */

                    // find fields from main column
                    /*
                    var linesFieldInMainColumn = linesFieldUnderNameMerged.Where(l =>
                        ((decimal)l.ExtGetLeft() <= (decimal)leftMiddleOfFields)
                        //&& (lineIDNum == null || lineIDNum != l)
                        && (!heightName.HasValue || ((decimal)l.ExtGetHeight() > (decimal)(heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM))));
                    */
                    //IList<Line> linesFieldInMainColumnMerged = MergeLinesInSameYPosIntoOneLine(linesFieldInMainColumn.ToList());
                    IList<Line> linesFieldInMainColumnMerged = MergeLinesInSameYPosIntoOneLine(linesFieldUnderNameMerged.ToList());

                    int numLinesInMainColumn = linesFieldInMainColumnMerged.Count();
                    //int idxMainColumn = 0;
                    // find fields from main column
                    //foreach (Line line in linesFieldInMainColumnMerged)
                    for(int idxMainColumn = 0; idxMainColumn < linesFieldInMainColumnMerged.Count; idxMainColumn++)
                    {
                        Line line = linesFieldInMainColumnMerged[idxMainColumn];
                        string text = line.Text.Trim();

                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()}");
                        /*
                        if (heightName.HasValue)
                        {
                            if (line.ExtGetHeight() < heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * {FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} = {heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} --> ignored");
                                numLinesInMainColumn--;
                                continue;
                            }
                        }
                        */
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesInMainColumn:{numLinesInMainColumn}");
                        // the 2nd last line is postcode and city
                        if (idxMainColumn + 2 == numLinesInMainColumn)
                        {
                            // POSTCODE CITY
                            string postcode_city = text;

                            string[] token = postcode_city.Split(separatorBlank, 2);
                            if (token.Length > 1)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                                if (regexFiveDigitsNumber.Match(token[0]).Success)
                                {
                                    POSTCODE = token[0];
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] !!! {token[0]} is not valid POSTCODE !!!");
                                }

                                CITY = token[1];
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> city_postcode: {postcode_city}");
                            }
                            idxMainColumn++;
                            continue;
                        }
                        // the last line is state
                        if (idxMainColumn + 1 == numLinesInMainColumn)
                        {
                            // STATE
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> STATE");
                            STATE = line.Text;
                            idxMainColumn++;
                            continue;
                        }
#if false
                        if (string.IsNullOrEmpty(NAME))
                        {
                            // NAME is under DRIVING_LICENCE
                            if ((labelDRIVING_LICENCE.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelDRIVING_LICENCE.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelDRIVING_LICENCE.Bottom) < labelDRIVING_LICENCE.Height * 4
                                && labelDRIVING_LICENCE.Height < line.ExtGetHeight())
                                )
                            || (labelDRIVING.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelDRIVING.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelDRIVING.Bottom) < labelDRIVING.Height * 4
                                && labelDRIVING.Height < line.ExtGetHeight())
                                )
                            || (labelLICENCE.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelLICENCE.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelLICENCE.Bottom) < labelLICENCE.Height * 6
                                && labelLICENCE.Height < line.ExtGetHeight())
                                )
                            || (labelMALAYSIA.IsLabelFound &&
                                ((double)(line.ExtGetBottom() - labelMALAYSIA.Bottom) > 0
                                && (double)(line.ExtGetBottom() - labelMALAYSIA.Bottom) < labelMALAYSIA.Height * 3
                                ))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NAME");
                                NAME = line.Text;
                                heightName = line.ExtGetHeight();
                                bottomName = line.ExtGetBottom();
                                heightFilter = (double)(heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM);
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] heightFilter = {heightFilter}");
                                idxMainColumn++;
                                continue;
                            }
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN but expect NAME here");
                        }
#endif  
                        switch (idxMainColumn)
                        {
                            case 0: // NATIONALITY, and IDNUM
                                Line lineUpper = new Line(line.BoundingBox, line.Text.ToUpper().Trim());
                                string[] words = lineUpper.Text.Split(separatorBlank, 2, StringSplitOptions.RemoveEmptyEntries);
                                if(words != null)
                                {
                                    if (string.IsNullOrEmpty(NATIONALITY))
                                    {
                                        if (words.Length >= 1)
                                        {
                                            // Warganegara_Nationality
                                            if (regexNationality.Match(words[0]).Success)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {words[0]} --> NATIONALITY");
                                                // (CITIZENSHIP) nationality is "MALAYSIA" or 3 letter code
                                                if (CheckCharInLine(words[0], "MALAYSIA"))
                                                {
                                                    NATIONALITY = "MY";
                                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MY --> NATIONALITY");
                                                }
                                                else
                                                {
                                                    NATIONALITY = words[0];
                                                }
                                            }
                                        }
                                    }
                                    if (string.IsNullOrEmpty(IDNUM))
                                    {
                                        if (words.Length >= 2)
                                        {
                                            string idnum = words[1].Trim().Replace(" ", "");
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {idnum} --> IDNUM");
                                            IDNUM = idnum;
                                            //lineIDNum = line;
                                        }
                                    }
                                }
                                break;
                            case 1: // CLASS
                                if (string.IsNullOrEmpty(CLASS))
                                {
                                    // check if the line is field of 'Kelas/Class'
                                    // https://en.wikipedia.org/wiki/Driving_licence_in_Malaysia#Classes
                                    // A, A1, B, B1, B2, C, D, DA, E, E1, E2, F, G, H, I, M
                                    bool isNotValueOfClass = false;
                                    string[] tokens = line.Text.Split(' ');
                                    foreach (string token in tokens)
                                    {
                                        if (token.Length > 2)
                                        {
                                            isNotValueOfClass = true;
                                            break;
                                        }

                                        char c = token[0];
                                        if ((c < 'A' || 'M' < c)
                                            && (c != '4' /* A */ && c != '8' /* B */ && c != 'c' /* C */ && c != '1' /* I */ && c != 'l' /* I */))
                                        {
                                            isNotValueOfClass = true;
                                            break;
                                        }
                                    }

                                    // Kelas_Class
                                    if (!isNotValueOfClass)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CLASS");
                                        CLASS = line.Text;
                                    }
                                    // maybe it is line of CLASS, but not scanned properly
                                    CLASS = line.Text;
                                }
                                break;
                            case 2: // VALID_FROM, VALID_UNTIL
                                if (string.IsNullOrEmpty(VALID_FROM))
                                {
                                    // Tempoh_Validity    dd/MM/yyyy - dd/MM/yyyy
                                    if (regexValidFromValidUntil.Match(line.Text).Success)
                                    {
                                        string line_validity = line.Text;
                                        string nums = "";
                                        foreach (char c in line_validity)
                                        {
                                            if (c <= '9' && c >= '0')
                                                nums += c;
                                        }
                                        if (nums.Length == 16)
                                        {
                                            //dd/MM/yyyy
                                            VALID_FROM = nums.Substring(0, 2) + "/" + nums.Substring(2, 2) + "/" + nums.Substring(4, 4);
                                            VALID_UNTIL = nums.Substring(8, 2) + "/" + nums.Substring(10, 2) + "/" + nums.Substring(12, 4);
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> VALID_FROM:{VALID_FROM} VALID_UNTIL:{VALID_UNTIL}");
                                        }
                                        continue;
                                    }

                                    if (regexValidDate.Match(line.Text).Success)
                                    {
                                        string line_validity = line.Text;
                                        string nums = "";
                                        foreach (char c in line_validity)
                                        {
                                            if (c <= '9' && c >= '0')
                                                nums += c;
                                        }
                                        if (nums.Length == 8)
                                        {
                                            //dd/MM/yyyy
                                            VALID_FROM = nums.Substring(0, 2) + "/" + nums.Substring(2, 2) + "/" + nums.Substring(4, 4);
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> VALID_FROM:{VALID_FROM}");
                                        }
                                    }
                                    /*
                                    if (idxMainColumn == 3)
                                    {
                                        // maybe it is line of VALID_FROM, but not scanned properly
                                        VALID_FROM = line.Text;
                                        continue;
                                    }
                                    */
                                }

                                if (string.IsNullOrEmpty(VALID_UNTIL))
                                {
                                    if (regexValidDate.Match(line.Text).Success)
                                    {
                                        string line_validity = line.Text;
                                        string nums = "";
                                        foreach (char c in line_validity)
                                        {
                                            if (c <= '9' && c >= '0')
                                                nums += c;
                                        }
                                        if (nums.Length == 8)
                                        {
                                            //dd/MM/yyyy
                                            VALID_UNTIL = nums.Substring(0, 2) + "/" + nums.Substring(2, 2) + "/" + nums.Substring(4, 4);
                                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> VALID_UNTIL:{VALID_UNTIL}");
                                        }
                                    }
                                }
                                break;
                            default: // ADDRESS...
                                if (string.IsNullOrEmpty(ADDRESS1))
                                {
                                    //if (labelAlamat_Address.IsLabelFound
                                    //   && (labelAlamat_Address.IsFieldJustUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                        ADDRESS1 = line.Text;
                                    }
                                }
                                else if (string.IsNullOrEmpty(ADDRESS2))
                                {
                                    //if (labelAlamat_Address.IsLabelFound
                                    //   && (labelAlamat_Address.IsFieldUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                        ADDRESS2 = line.Text;
                                    }
                                }
                                else if (string.IsNullOrEmpty(ADDRESS3))
                                {
                                    //if (labelAlamat_Address.IsLabelFound
                                    //   && (labelAlamat_Address.IsFieldUnderTheLabel(line) && labelAlamat_Address.IsFieldInSameLeftEdge(line)))
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                                        ADDRESS3 = line.Text;
                                    }
                                }

                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                                break;
                        } //switch
                    }// foreach lines in main column
#if false
                    // find fields from other (right aligned) column
                    int numLinesOutOfMainColumn = linesOutOfMainColumn.Count();
                    int idxOutOfMainColumn = 0;
                    foreach (Line line in linesOutOfMainColumn)
                    {
                        string text = line.Text.Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesOutMainColumn {line.Text} Height:{line.ExtGetHeight()}");

                        if (heightName.HasValue)
                        {
                            if (line.ExtGetHeight() < heightName * 0.65)
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * {FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} = {heightName * FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM} --> ignored");
                                numLinesOutOfMainColumn--;
                                continue;
                            }
                        }

                        if (string.IsNullOrEmpty(IDNUM))
                        {
                            // IDNUM is under NAME
                            if (!string.IsNullOrEmpty(NAME) &&
                                ((double)(line.ExtGetBottom() - bottomName) > 0
                                && (double)(line.ExtGetBottom() - bottomName) < heightName * 4
                                && Math.Abs((decimal)heightName - (decimal)line.ExtGetHeight()) < (decimal)(heightName / 2))
                                )
                            {
                                // No_Pengenalan_Identity_No
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> IDNUM");
                                IDNUM = line.Text;
                                idxOutOfMainColumn++;
                                continue;
                            }
                        }
                    }// foreach lines in other columns
#endif
                } // linesField.Count > 0

                // map to result and convert format 
                List<string> lsMissingFields = new List<string>();
                // NAME -> lastNameOrFullName 
                result.lastNameOrFullName = NAME;
                if (string.IsNullOrEmpty(NAME)) lsMissingFields.Add("NAME");

                // IDNUM -> documentNumber
                result.documentNumber = IDNUM;
                if (string.IsNullOrEmpty(IDNUM)) lsMissingFields.Add("IDNUM");

                // (CITIZENSHIP) nationality is "MALAYSIA" or 3 letter code
                result.nationality = NATIONALITY;
                if (string.IsNullOrEmpty(NATIONALITY)) lsMissingFields.Add("NATIONALITY");

                try
                {
                    result.documentIssueDate = "";
#if false
                // VALID_FROM "dd/MM/yyyy" -> documentIssueDate "yyyy-MM-dd"
                if (VALID_FROM.Length == 10)
                {
                    int dd = int.Parse(VALID_FROM.Substring(0, 2));
                    int MM = int.Parse(VALID_FROM.Substring(3, 2));
                    int yyyy = int.Parse(VALID_FROM.Substring(6, 4));
                    result.documentIssueDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("VALID_FROM");
                }
#else
                    if (string.IsNullOrEmpty(VALID_FROM))
                        lsMissingFields.Add("VALID_FROM");
                    else
                        result.documentIssueDate = VALID_FROM;
#endif
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    lsMissingFields.Add("VALID_FROM");
                }

                try
                {
                    result.documentExpirationDate = "";
#if false
                // VALID_UNTIL "dd/MM/yyyy" -> documentExpirationDate "yyyy-MM-dd"
                if (VALID_UNTIL.Length == 10)
                {
                    int dd = int.Parse(VALID_UNTIL.Substring(0, 2));
                    int MM = int.Parse(VALID_UNTIL.Substring(3, 2));
                    int yyyy = int.Parse(VALID_UNTIL.Substring(6, 4));
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("VALID_UNTIL");
                }
#else
                    if (string.IsNullOrEmpty(VALID_UNTIL))
                        lsMissingFields.Add("VALID_UNTIL");
                    else
                        result.documentExpirationDate = VALID_UNTIL;
#endif
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    lsMissingFields.Add("VALID_UNTIL");
                }

                // ADDRESS1, ADDRESS2, ADDRESS3, CITY, STATE -> addressLine1, addressLine2
                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    result.addressLine2 = $"{CITY} {STATE}";
                }
                else
                {
                    result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
                }

                // POSTCODE
                if (!string.IsNullOrEmpty(POSTCODE))
                {
                    result.postcode = POSTCODE;
                }
                else
                {
                    lsMissingFields.Add("POSTCODE");
                }

                // determine success or not
                if (lsMissingFields.Count == 0)
                {
                    result.Success = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfMYDL result NOT success");
                    if (lsMissingFields.Count > 0)
                    {
                        string fields = "";
                        foreach (string field in lsMissingFields)
                        {
                            if (!string.IsNullOrEmpty(fields))
                                fields += ",";
                            fields += field;
                        }
                        result.Error = $"Failed to scan [{fields}]";
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw ex;
            }
        }

        public static ScanMyKadResult ExtractFieldsFromReadResultOfMyKad(IList<Line> linesAll, SkiaSharp.SKImage bmpImage = null)
        {
            const string KAD_PENGENALAN = "KAD PENGENALAN";
            const string MALAYSIA = "MALAYSIA";
            const string IDENTITY_CARD = "IDENTITY CARD";
            char[] separatorBlank = { ' ' };

            ScanMyKadResult result = new ScanMyKadResult();

            //const double FILTER_WEAK_TEXT_SMALLER_THAN_IDNUM = 0.75f;
            const double FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM = 0.5f;
            int idxOf_KAD_PENGENALAN = -1;
            int idxOf_MALAYSIA = -1;
            int idxOf_IDENTITY_CARD = -1;
            string IDNUM = "";
            string NAME = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string POSTCODE = "";
            string CITY = "";
            string STATE = "";
            string CITIZENSHIP = "";
            string GENDER = "";
            string EASTMSIAN = "";
            string BIRTHDATE = "";

            var linesLeftOrder = linesAll.OrderBy(l => l.ExtGetLeft());
            double? leftEdgeOfBlock = linesAll.Min(l => l.ExtGetLeft());
            double? rightEdgeOfBlock = linesAll.Max(l => l.ExtGetRight());
            double? topEdgeOfBlock = linesAll.Min(l => l.ExtGetTop());
            double? bottomEdgeOfBlock = linesAll.Max(l => l.ExtGetBottom());
            double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.ExtGetLeft());
            double? avgLeft = sumLeft / 5;
            double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            //double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            // pick the lines aligned to left 
            //var linesLeftSide = linesAll.Where(l => l.ExtGetLeft() < h_leftSideEdge);
            var linesLeftSide = linesAll.Where(l => l.ExtGetLeft() < h_center);
            linesLeftSide = MergeLinesInSameYPosIntoOneLine(linesLeftSide.ToList());
            if (linesLeftSide.Any())
            {
                // sort from top to bottom
                linesLeftSide = linesLeftSide.OrderBy(l => l.ExtGetTop());
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{6}-\d{2}-\d{4}");
                int idxIdNum = -1;
                decimal heightIdNum = 0;
                Line[] arrayLinesLeftSide = linesLeftSide.ToArray();
                List<Line> lsLinesLeftSideValid = new List<Line>();
                int numLines = arrayLinesLeftSide.Length;
                for (int idx = 0; idx < arrayLinesLeftSide.Length; idx++)
                {
                    Line line = arrayLinesLeftSide[idx];
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
#if false
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                    }
#else
                    heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif
                    //List<double> conconfidencesOfWords = new List<double>();
                    //foreach (var word in line.Words)
                    //{
                    //    conconfidencesOfWords.Add(word.Confidence);
                    //}
                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text} -> Angle:{angle}");

                    try
                    {
                        if (regexIDNum.Match(text).Success)
                        {
                            idxIdNum = idx;
                            heightIdNum = heightLine;
                            lsLinesLeftSideValid.Add(line);
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   idxIdNum:{idxIdNum} heightIdNum:{heightIdNum}");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }

                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        numLines--;
                        continue;
                    }

                    if (idxIdNum == -1)
                    {
                        string strRegex = ".";
                        int countCharNotInKAD_PENGENALAN = 0;
                        int countCharInKAD_PENGENALAN = 0;
                        int countCharNotInMALAYSIA = 0;
                        int countCharInMALAYSIA = 0;
                        int countCharNotInIDENTITY_CARD = 0;
                        int countCharInIDENTITY_CARD = 0;
                        string textUpper = text.ToUpper();
                        foreach (char c in textUpper)
                        {
                            strRegex += $"{c}?";
                            if (!KAD_PENGENALAN.Contains(c))
                                countCharNotInKAD_PENGENALAN++;
                            else
                                countCharInKAD_PENGENALAN++;
                            if (!MALAYSIA.Contains(c))
                                countCharNotInMALAYSIA++;
                            else
                                countCharInMALAYSIA++;
                            if (!IDENTITY_CARD.Contains(c))
                                countCharNotInIDENTITY_CARD++;
                            else
                                countCharInIDENTITY_CARD++;
                        }
                        strRegex += ".";
                        Regex regexLine = new Regex(strRegex);
                        if (countCharNotInKAD_PENGENALAN < 3 && countCharInKAD_PENGENALAN > KAD_PENGENALAN.Length - 3 && regexLine.Match(KAD_PENGENALAN).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> KAD_PENGENALAN");
                            idxOf_KAD_PENGENALAN = idx;
                        }
                        else if (countCharNotInMALAYSIA < 3 && countCharInMALAYSIA > MALAYSIA.Length - 3 && regexLine.Match(MALAYSIA).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> MALAYSIA");
                            idxOf_MALAYSIA = idx;
                        }
                        else if (countCharNotInIDENTITY_CARD < 3 && countCharInIDENTITY_CARD > IDENTITY_CARD.Length - 3 && regexLine.Match(IDENTITY_CARD).Success)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> IDENTITY_CARD");
                            idxOf_IDENTITY_CARD = idx;
                        }
                        else
                        {
                            if(idxOf_KAD_PENGENALAN == 0 && idxOf_MALAYSIA == 1 && idxOf_IDENTITY_CARD == 2 && idx == 3)
                            {
                                // format is not expected, but this line must be IDNUM
                                idxIdNum = idx;
                                heightIdNum = heightLine;
                                //lsLinesLeftSideValid.Add(line);
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> IDNUM");
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   idxIdNum:{idxIdNum} heightIdNum:{heightIdNum}");
                                IDNUM = text;
                                // DOB is first 6 digit
                                BIRTHDATE = text.Substring(0, 6);
                            }
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> UNKNOWN");
                        }
                    }
                    else
                    {
                        // lines under IDNUM contains what we need...

                        if ((double)heightLine < (double)heightIdNum * FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] heightLine: {heightLine} < (heightIdNum: {heightIdNum})* {FILTER_TEXT_SMALLER_COMPARE_TO_IDNUM} --> Ignore this line.");
                            numLines--;
                            continue;
                        }

                        lsLinesLeftSideValid.Add(line);
                    }
                }// foreach

#if false
                if (bmpImage != null)
                {
                    List<Line> linesTess = new List<Line>();
                    foreach (Line line in lsLinesLeftSideValid)
                    {
                        SkiaSharp.SKImage imageLine = null;
                        SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                        if (line.BoundingBox.Count == 4)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[2], (int)line.BoundingBox[3]);
                            imageLine = bmpImage.Subset(rect);
                        }
                        else if (line.BoundingBox.Count == 8)
                        {
                            rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                            imageLine = bmpImage.Subset(rect);
                        }

                        if (rect.IsEmpty)
                            continue;

                        if (imageLine == null)
                            continue;

                        SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var lines = OCRLinesWithTesseractEncodedData(skData.ToArray());
                        foreach (Line lineTess in lines)
                        {
                            linesTess.Add(lineTess);
                        }
                    }
                    lsLinesLeftSideValid = linesTess;
                }
#endif

                numLines = lsLinesLeftSideValid.Count;
                for (int idx = 0; idx < lsLinesLeftSideValid.Count; idx++)
                {
                    Line line = lsLinesLeftSideValid[idx];
                    string text = line.Text.Trim();

                    if (numLines - 2 == idx)
                    {
                        // the 2nd last line is POSTCODE CITY
                        string postcode_city = text;
                        string[] token = postcode_city.Split(separatorBlank, 2);
                        if (token.Length > 1)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                            POSTCODE = token[0];
                            CITY = token[1];
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> postcode_city: {postcode_city}");
                        }
                    }
                    else if (numLines - 1 == idx)
                    {
                        // the last line is STATE
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> STATE: {text}");
                        STATE = line.Text.Trim();
                    }
                    else
                    {
                        switch (idx)
                        {
                            case 0:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> IDNUM");
                                IDNUM = text;
                                // DOB is first 6 digit
                                BIRTHDATE = text.Substring(0, 6);
                                break;
                            case 1:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> NAME");
                                NAME = text;
                                break;
                            case 2:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS1");
                                ADDRESS1 = text;
                                break;
                            case 3:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS2");
                                ADDRESS2 = text;
                                break;
                            case 4:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ADDRESS3");
                                ADDRESS3 = text;
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> ??UNKNOWN??");
                                break;
                        }
                    }
                }
            }

            // pick the lines aligned to right 
            var linesRightButtomSide = linesAll.Where(l => l.ExtGetLeft() >= h_center && l.ExtGetTop() >= v_center);
            if (linesRightButtomSide.Any())
            {
                const string WARGANEGARA = "WARGANEGARA";
                const string LELAKI = "LELAKI";
                const string PEREMPUAN = "PEREMPUAN";
                // sort from top to bottom
                linesRightButtomSide = linesRightButtomSide.OrderBy(l => l.ExtGetTop());
                System.Diagnostics.Debug.WriteLine("\nLines aligned to right:");
                int numLines = linesRightButtomSide.Count();
                for (int i = 0; i < numLines; i++)
                {
                    Line line = linesRightButtomSide.ElementAt(i);
                    string text = line.Text.Trim();
                    string strRegex = ".";
                    int countCharNotInWARGANEGARA = 0;
                    int countCharInWARGANEGARA = 0;
                    int countCharNotInLELAKI = 0;
                    int countCharInLELAKI = 0;
                    int countCharNotInPEREMPUAN = 0;
                    int countCharInPEREMPUAN = 0;
                    foreach (char c in text)
                    {
                        strRegex += $"{c}?";
                        if (!WARGANEGARA.Contains(c))
                            countCharNotInWARGANEGARA++;
                        else
                            countCharInWARGANEGARA++;
                        if (!LELAKI.Contains(c))
                            countCharNotInLELAKI++;
                        else
                            countCharInLELAKI++;
                        if (!PEREMPUAN.Contains(c))
                            countCharNotInPEREMPUAN++;
                        else
                            countCharInPEREMPUAN++;
                    }
                    strRegex += ".";

                    try
                    {
                        Regex regexLine = new Regex(strRegex);
                        if (countCharNotInWARGANEGARA < 3 && countCharInWARGANEGARA > WARGANEGARA.Length - 3 && regexLine.Match(WARGANEGARA).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> WARGANEGARA");
                            CITIZENSHIP = text;
                        }
                        else if (countCharNotInLELAKI < 3 && countCharInLELAKI > LELAKI.Length - 3 && regexLine.Match(LELAKI).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> LELAKI");
                            GENDER = "LELAKI";
                        }
                        else if (countCharNotInPEREMPUAN < 3 && countCharInPEREMPUAN > PEREMPUAN.Length - 3 && regexLine.Match(PEREMPUAN).Success)
                        {
                            System.Diagnostics.Debug.WriteLine("--> PEREMPUAN");
                            GENDER = "PEREMPUAN";
                        }
                        else if (text == "H" || text == "K")
                        {
                            System.Diagnostics.Debug.WriteLine("--> EAST_M");
                            EASTMSIAN = text;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("--> UNKNOWN");
                        }

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                }
            }


            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            result.lastNameOrFullName = NAME;
            if (string.IsNullOrEmpty(NAME)) lsMissingFields.Add("NAME");

            // IDNUM -> documentNumber
            result.documentNumber = IDNUM;
            if (string.IsNullOrEmpty(IDNUM)) lsMissingFields.Add("IDNUM");

            // (CITIZENSHIP) nationality is "MY" (by default)

            // BIRTHDATE "yyMMdd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                if (!string.IsNullOrEmpty(BIRTHDATE))
                {
                    int yy = int.Parse(BIRTHDATE.Substring(0, 2));
                    int MM = int.Parse(BIRTHDATE.Substring(2, 2));
                    int dd = int.Parse(BIRTHDATE.Substring(4, 2));
                    //https://www.ibm.com/docs/en/i/7.2?topic=mcdtdi-conversion-2-digit-years-4-digit-years-centuries
                    // If the 2-digit year is greater than or equal to 40, the century used is 1900. In other words, 19 becomes the first 2 digits of the 4-digit year.
                    // If the 2 - digit year is less than 40, the century used is 2000.In other words, 20 becomes the first 2 digits of the 4 - digit year.
                    if (yy >= 40)
                        result.dateOfBirth = $"{(1900 + yy):0000}-{MM:00}-{dd:00}";
                    else
                        result.dateOfBirth = $"{(2000 + yy):0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("BIRTHDATE");
                }
            }
            catch (Exception e)
            {
                result.dateOfBirth = "";
                lsMissingFields.Add("BIRTHDATE");
            }

            // GENDER -> gender
            switch (GENDER)
            {
                case "LELAKI":
                    result.gender = "M";
                    break;
                case "PEREMPUAN":
                    result.gender = "F";
                    break;
                default:
                    result.gender = "";
                    lsMissingFields.Add("GENDER");
                    break;
            }

            result.documentExpirationDate = null;

            result.documentIssueDate = null;

            // ADDRESS1, ADDRESS2, ADDRESS3, STATE -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            if (string.IsNullOrEmpty(ADDRESS3))
            {
                result.addressLine1 = ADDRESS1;
                result.addressLine2 = $"{ADDRESS2} {CITY} {STATE}";
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";
                result.addressLine2 = $"{ADDRESS3} {CITY} {STATE}";
            }

            // POSTCODE
            result.postcode = POSTCODE;
            if (string.IsNullOrEmpty(POSTCODE)) lsMissingFields.Add("POSTCODE");

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfMyKad result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }

        class GroupOfLine
        {
            public GroupOfLine() { }
            public GroupOfLine(List<Line> lines) { Lines = lines; }

            public List<Line> Lines { get; } = new List<Line>();
            public double? Left
            {
                get
                {
                    return Lines.Select(l => l.ExtGetLeft()).Min();
                }
            }
            public double? Right
            {
                get
                {
                    return Lines.Select(l => l.ExtGetRight()).Max();
                }
            }
            public double? AvgLeft
            {
                get
                {
                    return Lines.Select(l => l.ExtGetLeft()).Average();
                }
            }
            public double? AvgTop
            {
                get
                {
                    return Lines.Select(l => l.ExtGetTop()).Average();
                }
            }
            public double? AvgRight
            {
                get
                {
                    return Lines.Select(l => l.ExtGetRight()).Average();
                }
            }
            public double? AvgBottom
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBottom()).Average();
                }
            }
            public double? AvgHeight
            {
                get
                {
                    return Lines.Select(l => l.ExtGetHeight()).Average();
                }
            }
            public double? AvgWidth
            {
                get
                {
                    return Lines.Select(l => l.ExtGetWidth()).Average();
                }
            }
            public double? AvgBaselineSlope
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBaselineSlope()).Average();
                }
            }
            public double? AvgInterceptWithYAxis
            {
                get
                {
                    return Lines.Select(l => l.ExtGetBaselineInterceptWithYAxis()).Average();
                }
            }
        }
#if false
        static IList<Line> MergeLinesInSameYPosIntoOneLine(IList<Line> linesAll)
        {
            List<GroupOfLine> lsLineGroupOnTheSameLine = new List<GroupOfLine>();
            List<Line> linesInTheSameLine = new List<Line>();
            List<Line> linesSorted = linesAll.OrderBy(l => l.ExtGetBottom()).ToList();
            //double? prevBottom = null;
            //double? prevTop = null;
            GroupOfLine curGroup = null;
            foreach (Line line in linesSorted)
            {
                if (curGroup == null)
                {
                    //prevBottom = line.ExtGetBottom();
                    //prevTop = line.ExtGetTop();
                    curGroup = new GroupOfLine(new List<Line>() { line });
                    lsLineGroupOnTheSameLine.Add(curGroup);
                }
                else if (line.ExtGetBottom() != null && line.ExtGetTop() != null && line.ExtGetHeight() != null)
                {
                    double gapAllowed = (double)(line.ExtGetHeight() / 5);
                    if (Math.Abs((decimal)(line.ExtGetBottom() - curGroup.AvgBottom)) < (decimal)gapAllowed && Math.Abs((decimal)(line.ExtGetTop() - curGroup.AvgTop)) < (decimal)gapAllowed)
                    {
                        curGroup.Lines.Add(line);
                        /*
                        foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
                        {
                            if(groupOfLine.AvgTop != null && groupOfLine.AvgBottom != null)
                            {
                                if ((Math.Abs((decimal)(groupOfLine.AvgTop - line.ExtGetTop())) < (decimal)gapAllowed)
                                 && (Math.Abs((decimal)(groupOfLine.AvgBottom - line.ExtGetBottom())) < (decimal)gapAllowed)
                                    )
                                {
                                    groupOfLine.Lines.Add(line);
                                    break;
                                }
                            }
                        }
                        */
                    }
                    else
                    {
                        curGroup = new GroupOfLine(new List<Line>() { line });
                        lsLineGroupOnTheSameLine.Add(curGroup);
                    }
                }
            }

            foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
            {
                //List<Line> lsInTheSameLine = dictLinesInTheSameLine[bottom];
                List<Line> linesSortedFromLeftToRight = groupOfLine.Lines.OrderBy(l => l.ExtGetLeft()).ToList();
                Line lineConcat = null;
                foreach (Line l in linesSortedFromLeftToRight)
                {
                    if (lineConcat == null)
                    {
                        lineConcat = l;
                    }
                    else
                    {
                        try
                        {
                            lineConcat = lineConcat.MergedLine(l);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Exception {ex}");
                        }
                    }
                }
                if (lineConcat != null)
                {
                    linesInTheSameLine.Add(lineConcat);
                }
            }

            return linesInTheSameLine;
        }
#else
        static IList<Line> MergeLinesInSameYPosIntoOneLine(IList<Line> linesAll, double? holizonalGapAllowedPerLineHeight = null)
        {
            List<GroupOfLine> lsLineGroupOnTheSameLine = new List<GroupOfLine>();
            List<Line> linesInTheSameLine = new List<Line>();
            //List<Line> linesSorted = linesAll.OrderBy(l => l.ExtGetBottom()).ToList();
            List<Line> linesSorted = linesAll.ToList();
            //double? prevBottom = null;
            //double? prevTop = null;
            GroupOfLine curGroup = null;
            foreach (Line line in linesSorted)
            {
                if (curGroup == null)
                {
                    //prevBottom = line.ExtGetBottom();
                    //prevTop = line.ExtGetTop();
                    curGroup = new GroupOfLine(new List<Line>() { line });
                    lsLineGroupOnTheSameLine.Add(curGroup);
                }
                else if (line.ExtGetBottom() != null && line.ExtGetTop() != null && line.ExtGetHeight() != null)
                {
                    double gapAllowedVertically = (double)(line.ExtGetHeight() / 3);
                    // double gapAllowedHorizontally = (double)(line.ExtGetHeight() * 3);
                    double gapAllowedHorizontally = (double)((holizonalGapAllowedPerLineHeight.HasValue)
                        ? (holizonalGapAllowedPerLineHeight.Value * line.ExtGetHeight()) : double.MaxValue);
                    double? curGroupAvgSlope = curGroup.AvgBaselineSlope;
                    double? curGroupAvgInterceptWithYAxis = curGroup.AvgInterceptWithYAxis;
                    bool isOnSameLine = false;
                    if(curGroupAvgSlope != null && curGroupAvgInterceptWithYAxis != null && line.ExtGetBaselineSlope() != null && line.ExtGetBaselineInterceptWithYAxis() != null)
                    {
                        double? slopeLine = line.ExtGetBaselineSlope();
                        double? interceptWithYAxisLine = line.ExtGetBaselineInterceptWithYAxis();
                        if (Math.Abs((decimal)(curGroupAvgSlope - slopeLine)) < (decimal)0.5 
                        && Math.Abs((decimal)(curGroupAvgInterceptWithYAxis - interceptWithYAxisLine)) < (decimal)gapAllowedVertically)
                        {
                            isOnSameLine = true;
                        }
                    }

                    if(!isOnSameLine)
                    {
                        if (Math.Abs((decimal)(line.ExtGetBottom() - curGroup.AvgBottom)) < (decimal)gapAllowedVertically
                            && Math.Abs((decimal)(line.ExtGetTop() - curGroup.AvgTop)) < (decimal)gapAllowedVertically
                            && ((curGroup.Left > line.ExtGetRight() && curGroup.Left - line.ExtGetRight() < gapAllowedHorizontally) // at left side 
                                || (line.ExtGetLeft() > curGroup.Right && line.ExtGetLeft() - curGroup.Right < gapAllowedHorizontally)  // at right side
                                || curGroup.Left < line.ExtGetLeft() && curGroup.Right > line.ExtGetRight()) // inside 
                                )
                        {
                            isOnSameLine = true;
                        }
                    }

                    if (isOnSameLine)
                    {
                        curGroup.Lines.Add(line);
                        /*
                        foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
                        {
                            if(groupOfLine.AvgTop != null && groupOfLine.AvgBottom != null)
                            {
                                if ((Math.Abs((decimal)(groupOfLine.AvgTop - line.ExtGetTop())) < (decimal)gapAllowed)
                                 && (Math.Abs((decimal)(groupOfLine.AvgBottom - line.ExtGetBottom())) < (decimal)gapAllowed)
                                    )
                                {
                                    groupOfLine.Lines.Add(line);
                                    break;
                                }
                            }
                        }
                        */
                    }
                    else
                    {
                        curGroup = new GroupOfLine(new List<Line>() { line });
                        lsLineGroupOnTheSameLine.Add(curGroup);
                    }
                }
            }

            foreach (GroupOfLine groupOfLine in lsLineGroupOnTheSameLine)
            {
                //List<Line> lsInTheSameLine = dictLinesInTheSameLine[bottom];
                List<Line> linesSortedFromLeftToRight = groupOfLine.Lines.OrderBy(l => l.ExtGetLeft()).ToList();
                Line lineConcat = null;
                foreach (Line l in linesSortedFromLeftToRight)
                {
                    if (lineConcat == null)
                    {
                        lineConcat = l;
                    }
                    else
                    {
                        try
                        {
                            lineConcat = lineConcat.MergedLine(l);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Exception {ex}");
                        }
                    }
                }
                if (lineConcat != null)
                {
                    linesInTheSameLine.Add(lineConcat);
                }
            }

            return linesInTheSameLine;
        }
#endif
        public static ScanPHUMIDResult ExtractFieldsFromReadResultOfPHUMID(IList<Line> linesAll, SkiaSharp.SKImage bmpImage = null)
        {
            LabelInfo labelREPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES");
            LabelInfo labelUnified_Multi_Purpose_ID = new LabelInfo("Unified Multi-Purpose ID");
            LabelInfo labelCRN = new LabelInfo("CRN-");
            LabelInfo labelSURNAME = new LabelInfo("SURNAME");
            LabelInfo labelGIVEN_NAME = new LabelInfo("GIVEN NAME");
            LabelInfo labelMIDDLE_NAME = new LabelInfo("MIDDLE NAME");
            LabelInfo labelSEX = new LabelInfo("SEX ?");
            LabelInfo labelSEX_OLD = new LabelInfo("SEX");
            LabelInfo labelDATE_OF_BIRTH_yyyy_MM_dd = new LabelInfo("DATE OF BIRTH yyyy/MM/dd");
            LabelInfo labelSEX_DATE_OF_BIRTH_yyyy_MM_dd = new LabelInfo("SEX ? DATE OF BIRTH yyyy/MM/dd");
            LabelInfo labelADDRESS = new LabelInfo("ADDRESS");

            ScanPHUMIDResult result = new ScanPHUMIDResult();

            string CRN = "";
            string SURNAME = "";
            string GIVEN_NAME = "";
            string GIVEN_NAME2 = "";
            string MIDDLE_NAME = "";
            string SEX = "";
            string DOB = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";
            string ADDRESS3 = "";
            string ADDRESS4 = "";
            string POSTCODE = "";

            double? heightLabel = null;
            double? heightField = null;

            IList<Line> linesInTheSameLine = MergeLinesInSameYPosIntoOneLine(linesAll);
            /*
            Dictionary<double, List<Line>> dictLinesInTheSameLine = new Dictionary<double, List<Line>>();
            List<Line> linesInTheSameLine = new List<Line>();
            List<Line> linesSorted = linesAll.OrderBy(l => l.BoundingBox[7]).ToList();
            double? prevBottom = null; ;
            foreach (Line line in linesSorted)
            {
                if (prevBottom == null)
                {
                    prevBottom = line.ExtGetBottom();
                    dictLinesInTheSameLine[prevBottom.Value] = new List<Line>() { line };
                }
                else
                {
                    if(line.ExtGetBottom() - prevBottom < (line.ExtGetHeight() / 5))
                    {
                        if (!dictLinesInTheSameLine.ContainsKey(prevBottom.Value))
                        {
                            dictLinesInTheSameLine[prevBottom.Value] = new List<Line>();
                        }
                        dictLinesInTheSameLine[prevBottom.Value].Add(line);
                    }
                    else
                    {
                        prevBottom = line.ExtGetBottom();
                        dictLinesInTheSameLine[prevBottom.Value] = new List<Line>() { line };
                    }
                }
            }

            foreach(double bottom in dictLinesInTheSameLine.Keys)
            {
                List<Line> lsInTheSameLine = dictLinesInTheSameLine[bottom];
                List<Line> linesSortedFromLeftToRight = lsInTheSameLine.OrderBy(l => l.BoundingBox[0]).ToList();
                Line? lineConcat = null;
                foreach(Line l in linesSortedFromLeftToRight)
                {
                    if(lineConcat == null)
                    {
                        lineConcat = l;
                    }
                    else
                    {
                        try
                        {
                            lineConcat = lineConcat.MergedLine(l);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Exception {ex}");
                        }
                    }
                }
                linesInTheSameLine.Add(lineConcat);
            }
            */

            IList<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label
            foreach (Line line in linesInTheSameLine)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesInTheSameLine {line.Text} Height:{line.ExtGetHeight()}");
                //if (confidence.Avg < 0.5)
                //{
                //    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                //    continue;
                //}

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                //"REPUBLIC OF THE PHILIPPINES"
                if (!labelREPUBLIC_OF_THE_PHILIPPINES.IsLabelFound)
                {
                    if (labelREPUBLIC_OF_THE_PHILIPPINES.MatchTitleExactly(line))
                    {
                        if(heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }
                //"Unified Multi-Purpose ID"
                if (!labelUnified_Multi_Purpose_ID.IsLabelFound)
                {
                    if (labelUnified_Multi_Purpose_ID.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }
                //"CRN-"
                if (!labelCRN.IsLabelFound)
                {
                    string lineNumeric = CorrectFalseParsedNumericLine(line.Text.Trim());
                    Line lineNumericField = new Line(line.BoundingBox, lineNumeric);
                    if (labelCRN.MatchTitleRegex(lineNumericField, "CRN\\s?-?\\s?\\d{4}\\s?-?\\s?\\d{7}\\s?-?\\s?\\d{1}"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CRN");
                        CRN = lineNumericField.Text;
                        heightField = lineNumericField.ExtGetHeight();   // heightField
                        continue;
                    }
                }
                //"SURNAME"
                if (!labelSURNAME.IsLabelFound)
                {
                    if (labelSURNAME.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }
                //"GIVEN NAME"
                if (!labelGIVEN_NAME.IsLabelFound)
                {
                    if (labelGIVEN_NAME.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }
                //"MIDDLE NAME"
                if (!labelMIDDLE_NAME.IsLabelFound)
                {
                    if (labelMIDDLE_NAME.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }

                //"SEX"
                if (!labelSEX.IsLabelFound)
                {
                    if (labelSEX.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }
                //"SEX" old format
                if (!labelSEX_OLD.IsLabelFound)
                {
                    if (labelSEX_OLD.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }

                //"DATE_OF_BIRTH_yyyy_MM_dd"
                if (!labelDATE_OF_BIRTH_yyyy_MM_dd.IsLabelFound)
                {
                    if (labelDATE_OF_BIRTH_yyyy_MM_dd.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }

                //"ADDRESS"
                if (!labelADDRESS.IsLabelFound)
                {
                    if (labelADDRESS.MatchTitleExactly(line))
                    {
                        if (heightLabel == null)
                            heightLabel = line.ExtGetHeight();
                        continue;
                    }
                }

                linesFieldOrLabel.Add(line);
            }// foreach lines 
#if false
            if(bmpImage != null)
            {
                List<Line> linesTess = new List<Line>();
                foreach (Line line in linesFieldOrLabel)
                {
                    SkiaSharp.SKImage imageLine  = null;
                    SkiaSharp.SKRectI rect = SkiaSharp.SKRectI.Empty;
                    if (line.BoundingBox.Count == 4)
                    {
                        rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[2], (int)line.BoundingBox[3]);
                        imageLine = bmpImage.Subset(rect);
                    }
                    else if(line.BoundingBox.Count == 8)
                    {
                        rect = new SkiaSharp.SKRectI((int)line.BoundingBox[0], (int)line.BoundingBox[1], (int)line.BoundingBox[4], (int)line.BoundingBox[5]);
                        imageLine = bmpImage.Subset(rect);
                    }

                    if(rect.IsEmpty)
                        continue;

                    if (imageLine == null) 
                        continue;
                    
                    SKData skData = imageLine.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var lines = OCRLinesWithTesseractEncodedData(skData.ToArray(), "eng+ocra");
                    foreach (Line lineTess in lines)
                    {
                        linesTess.Add(lineTess);
                    }
                }
                linesFieldOrLabel = linesTess;
            }
#endif
            List<Line> linesField = new List<Line>();
            foreach (Line line in linesFieldOrLabel)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesFieldOrLabel {line.Text} Height:{line.ExtGetHeight()}");
                //if (confidence.Avg < 0.5)
                //{
                //    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                //    continue;
                //}

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }
                /*
                if(heightLabel != null)
                {
                    double? height = line.ExtGetHeight();
                    if (heightLabel * 0.5 > height)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   height:{height} > heightLabel:{heightLabel} --> ignored");
                        continue;
                    }
                }
                */
                if (labelCRN.IsLabelFound)
                {
                    double? lineBottom = line.ExtGetBottom();
                    if (lineBottom < labelCRN.Top)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   lineBottom:{lineBottom} > labelCRN.Top:{labelCRN.Top} --> ignored");
                        continue;
                    }
                }

                if (labelSURNAME.IsLabelFound)
                {
                    double? lineBottom = line.ExtGetBottom();
                    if (lineBottom < labelSURNAME.Top)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   lineBottom:{lineBottom} > labelSURNAME.Top:{labelSURNAME.Top} --> ignored");
                        continue;
                    }
                }

                //"REPUBLIC OF THE PHILIPPINES"
                if (!labelREPUBLIC_OF_THE_PHILIPPINES.IsLabelFound)
                {
                    if (labelREPUBLIC_OF_THE_PHILIPPINES.MatchTitle(line /*, mSpellSuggestion*/))
                        continue;
                }
                //"Unified Multi-Purpose ID"
                if (!labelUnified_Multi_Purpose_ID.IsLabelFound)
                {
                    if (labelUnified_Multi_Purpose_ID.MatchTitle(line /*, mSpellSuggestion*/))
                        continue;
                }
                //"CRN-"
                if (!labelCRN.IsLabelFound)
                {
                    if (labelCRN.MatchTitleRegex(line, "CRN\\s?-?\\s?\\d{4}\\s?-?\\s?\\d{7}\\s?-?\\s?\\d{1}"))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CRN");
                        CRN = line.Text;
                        continue;
                    }
                }
                //"SURNAME"
                if (!labelSURNAME.IsLabelFound)
                {
                    if (labelSURNAME.MatchTitle(line /*, mSpellSuggestion*/))
                        continue;
                }
                //"GIVEN NAME"
                if (!labelGIVEN_NAME.IsLabelFound)
                {
                    if (labelGIVEN_NAME.MatchTitle(line /*, mSpellSuggestion*/))
                        continue;
                }
                //"MIDDLE NAME"
                if (!labelMIDDLE_NAME.IsLabelFound)
                {
                    if (labelMIDDLE_NAME.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }

                // in case SEX and DOB is in the same line like: "SEX ? DATE_OF_BIRTH_yyyy_MM_dd"
                if (!labelSEX.IsLabelFound
                 && !labelSEX_OLD.IsLabelFound
                 && !labelDATE_OF_BIRTH_yyyy_MM_dd.IsLabelFound
                 && !labelSEX_DATE_OF_BIRTH_yyyy_MM_dd.IsLabelFound)
                {
                    if (labelSEX_DATE_OF_BIRTH_yyyy_MM_dd.MatchTitleRegex(line, ".*[M|F][ ].*\\d{4}\\/\\d{2}\\/\\d{2}"))
                    {
                        bool bFound = false;
                        if (line.Text.Contains(" M"))
                        {
                            SEX = "M";
                            bFound = true;
                        }
                        else if (line.Text.Contains(" F"))
                        {
                            SEX = "F";
                            bFound = true;
                        }

                        try
                        {
                            Regex regexLine = new Regex("\\d{4}\\/\\d{2}\\/\\d{2}");
                            Match match = regexLine.Match(text.Replace(" ", ""));
                            if (match.Success)
                            {
                                DOB = match.Value;
                                bFound = true;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }

                        if (bFound)
                            continue;
                    }
                }

                //"ADDRESS"
                if (!labelADDRESS.IsLabelFound)
                {
                    if (labelADDRESS.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }

                linesField.Add(line);
            }// foreach lines 

            /*
            var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
            int countLinesField = linesField.Count;
            var linesInMainColumn = new List<Line>();
            var linesOutOfMainColumn = new List<Line>();
            int idxMedianLinesField = countLinesField / 2;
            if(linesLeftOrder.Count() > idxMedianLinesField)
            {
                double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
                double? leftMiddleOfFields = leftMedian;

                double? leftEdgeOfBlock = linesField.Min(l => l.BoundingBox[0]);
                double? rightEdgeOfBlock = linesField.Max(l => l.BoundingBox[2]);
                double? topEdgeOfBlock = linesField.Min(l => l.BoundingBox[1]);
                double? bottomEdgeOfBlock = linesField.Max(l => l.BoundingBox[5]);
                double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
                double? avgLeft = sumLeft / 5;
                double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
                double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
                double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
                double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

                linesInMainColumn = linesField.Where(l => Math.Abs((decimal)l.BoundingBox[0] - (decimal)leftMiddleOfFields) <= (decimal)acceptableRangeOfLeftEdge).ToList();
                linesOutOfMainColumn = linesField.Where(l => Math.Abs((decimal)l.BoundingBox[0] - (decimal)leftMiddleOfFields) > (decimal)acceptableRangeOfLeftEdge).ToList();
            }

            //double? heightName = null;
            //double? bottomName = null;
            // sort from top to bottom
            linesInMainColumn.OrderBy(l => l.BoundingBox[1]);
            int numLinesInMainColumn = linesInMainColumn.Count();
            int idxMainColumn = 0;
            */

            List<Line> linesFieldFiltered = linesField.Where(l => 
                (heightField == null || ((heightField * 0.8) < l.ExtGetHeight() && l.ExtGetHeight() < (heightField * 1.2)))
                && (!labelCRN.IsLabelFound || labelCRN.Bottom < l.ExtGetTop())
                ).ToList();
            var linesFieldMerged = MergeLinesInSameYPosIntoOneLine(linesFieldFiltered);
            int numLinesInMainColumn = linesFieldMerged.Count();
            int idxMainColumn = 0;
            var linesFieldSorted = linesFieldMerged.OrderBy(l => l.ExtGetTop());

            //foreach (Line line in linesInMainColumn)
            foreach (Line line in linesFieldSorted)
            {
                string text = line.Text.Trim();

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()}");

                //if (heightName.HasValue)
                //{
                //    if (line.ExtGetHeight() < heightName * 0.65)
                //    {
                //        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                //        numLinesInMainColumn--;
                //        continue;
                //    }
                //}

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesInMainColumn:{numLinesInMainColumn}");
                /*
                //"REPUBLIC OF THE PHILIPPINES"
                if (!labelREPUBLIC_OF_THE_PHILIPPINES.HasConfidence)
                {
                    if (labelREPUBLIC_OF_THE_PHILIPPINES.MatchTitleExactly(line))
                        continue;
                }
                //"Unified Multi-Purpose ID"
                if (!labelUnified_Multi_Purpose_ID.HasConfidence)
                {
                    if (labelUnified_Multi_Purpose_ID.MatchTitleExactly(line))
                        continue;
                }
                //"SURNAME"
                if (!labelSURNAME.HasConfidence)
                {
                    if (labelSURNAME.MatchTitle(line))
                        continue;
                }
                //"GIVEN NAME"
                if (!labelGIVEN_NAME.HasConfidence)
                {
                    if (labelGIVEN_NAME.MatchTitle(line))
                        continue;
                }
                //"MIDDLE NAME"
                if (!labelMIDDLE_NAME.HasConfidence)
                {
                    if (labelMIDDLE_NAME.MatchTitle(line))
                        continue;
                }

                //"SEX"
                if (!labelSEX.HasConfidence)
                {
                    if (labelSEX.MatchTitle(line))
                    {
                        if (string.IsNullOrEmpty(SEX))
                        {
                            if (line.Text.Contains(" M"))
                            {
                                SEX = "M";
                                confidence_SEX = confidence;
                            }
                            else if (line.Text.Contains(" F"))
                            {
                                SEX = "F";
                                confidence_SEX = confidence;
                            }
                            continue;
                        }
                    }
                }

                //"DATE_OF_BIRTH_yyyy_MM_dd"
                if (!labelDATE_OF_BIRTH_yyyy_MM_dd.HasConfidence)
                {
                    if (labelDATE_OF_BIRTH_yyyy_MM_dd.MatchTitle(line))
                    {
                        if (string.IsNullOrEmpty(DOB))
                        {
                            try
                            {
                                Regex regexLine = new Regex("\\d{4}\\/\\d{2}\\/\\d{2}");
                                Match match = regexLine.Match(text.Replace(" ", ""));
                                if (match.Success)
                                {
                                    DOB = match.Value;
                                    confidence_DOB = confidence;
                                    continue;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.Message);
                            }
                        }
                    }
                }

                //"ADDRESS"
                if (!labelADDRESS.HasConfidence)
                {
                    if (labelADDRESS.MatchTitle(line))
                        continue;
                }
                */
                if (string.IsNullOrEmpty(SURNAME))
                {
                    // SURNAME
                    if (!labelSURNAME.IsLabelFound
                      || labelSURNAME.IsFieldJustUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                        SURNAME = line.Text;
                        //heightName = line.ExtGetHeight();
                        //bottomName = line.ExtGetBottom();
                        idxMainColumn++;
                        continue;
                    }
                    if (!labelSURNAME.IsLabelFound
                      || labelSURNAME.IsFieldRightNextToTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                        SURNAME = line.Text;
                        //heightName = line.ExtGetHeight();
                        //bottomName = line.ExtGetBottom();
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(GIVEN_NAME))
                {
                    // GIVEN_NAME
                    if (!labelGIVEN_NAME.IsLabelFound
                        || labelGIVEN_NAME.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                        GIVEN_NAME = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                    if (!labelGIVEN_NAME.IsLabelFound
                        || labelGIVEN_NAME.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                        GIVEN_NAME = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(GIVEN_NAME2))
                {
                    // GIVEN_NAME
                    if (labelGIVEN_NAME.IsLabelFound && labelGIVEN_NAME.IsFieldJustUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME2");
                        GIVEN_NAME2 = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(MIDDLE_NAME))
                {
                    // MIDDLE_NAME
                    if (!labelMIDDLE_NAME.IsLabelFound
                        || labelMIDDLE_NAME.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                        MIDDLE_NAME = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                    if (!labelMIDDLE_NAME.IsLabelFound
                        || labelMIDDLE_NAME.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                        MIDDLE_NAME = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(CRN))
                {
                    // CRN
                    if (labelCRN.IsLabelFound)
                    {
                        // already found
                        idxMainColumn++;
                        continue;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> CRN (maybe...)");
                        CRN = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(SEX) || string.IsNullOrEmpty(DOB))
                {
                    bool bFound = false;
                    if (string.IsNullOrEmpty(SEX))
                    {
                        if (labelSEX.IsLabelFound && labelSEX.IsFieldRightNextToTheLabel(line))
                        {
                            if (line.Text.Contains(" M"))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "M";
                                bFound = true;
                            }
                            else if (line.Text.Contains(" F"))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "F";
                                bFound = true;
                            }
                        }
                        if (labelSEX_OLD.IsLabelFound && labelSEX_OLD.IsFieldRightNextToTheLabel(line))
                        {
                            if (line.Text.Trim().StartsWith("M")) //MALE
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "M";
                                bFound = true;
                            }
                            else if (line.Text.Trim().StartsWith("F"))  //FEMALE
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "F";
                                bFound = true;
                            }
                        }
                        if (!labelSEX.IsLabelFound && !labelSEX_OLD.IsLabelFound)
                        {
                            // old format
                            if (line.Text.Trim().StartsWith("M")) //MALE
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "M";
                                bFound = true;
                            }
                            else if (line.Text.Trim().StartsWith("F"))  //FEMALE
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "F";
                                bFound = true;
                            }

                            // new format
                            if (line.Text.Contains(" M"))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "M";
                                bFound = true;
                            }
                            else if (line.Text.Contains(" F"))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                                SEX = "F";
                                bFound = true;
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(DOB))
                    {
                        if (!labelDATE_OF_BIRTH_yyyy_MM_dd.IsLabelFound
                            || labelDATE_OF_BIRTH_yyyy_MM_dd.IsFieldRightNextToTheLabel(line))
                        {
                            try
                            {
                                Regex regexLine = new Regex("\\d{4}\\/\\d{2}\\/\\d{2}");
                                Match match = regexLine.Match(text.Replace(" ", ""));
                                if (match.Success)
                                {
                                    DOB = match.Value;
                                    bFound = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(ex.Message);
                            }
                        }
                    }

                    if (bFound)
                    {
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(ADDRESS1))
                {
                    if (!labelADDRESS.IsLabelFound || labelADDRESS.IsFieldJustUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                        ADDRESS1 = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(ADDRESS2))
                {
                    if (!labelADDRESS.IsLabelFound || labelADDRESS.IsFieldUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                        ADDRESS2 = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (!labelADDRESS.IsLabelFound || labelADDRESS.IsFieldUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS3");
                        ADDRESS3 = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(ADDRESS4))
                {
                    if (!labelADDRESS.IsLabelFound || labelADDRESS.IsFieldUnderTheLabel(line))
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS4");
                        ADDRESS4 = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                idxMainColumn++;
            }// foreach lines in main column

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // SURNAME -> lastNameOrFullName 
            result.lastNameOrFullName = SURNAME;
            if (string.IsNullOrEmpty(SURNAME)) lsMissingFields.Add("SURNAME");

            // GIVEN_NAME -> firstName 
            result.firstName = GIVEN_NAME;
            if (string.IsNullOrEmpty(GIVEN_NAME)) lsMissingFields.Add("GIVEN_NAME");

            if (!string.IsNullOrEmpty(GIVEN_NAME2))
            {
                result.firstName = GIVEN_NAME + " " + GIVEN_NAME2;
            }

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            result.documentNumber = CRN;
            if (string.IsNullOrEmpty(CRN)) lsMissingFields.Add("CRN");

            // (CITIZENSHIP) nationality is "PH" (by default)

            // SEX
            result.gender = SEX;

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                if (string.IsNullOrEmpty(DOB)) lsMissingFields.Add("DOB");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1)) lsMissingFields.Add("ADDRESS1");
            // extract post code 
            if (string.IsNullOrEmpty(ADDRESS4))
            {
                if (string.IsNullOrEmpty(ADDRESS3))
                {
                    if (string.IsNullOrEmpty(ADDRESS2))
                    {
                        int lenAddrLast = ADDRESS1.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS1.Length > 4)
                        {
                            addrLast = ADDRESS1.Substring(0, ADDRESS1.Length - 4);
                            last4 = ADDRESS1.Substring(ADDRESS1.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine1 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine1 = $"{ADDRESS1}";
                        }
                    }
                    else
                    {
                        result.addressLine1 = $"{ADDRESS1}";

                        int lenAddrLast = ADDRESS2.Length;
                        string addrLast = "";
                        string last4 = "";
                        if (ADDRESS2.Length > 4)
                        {
                            addrLast = ADDRESS2.Substring(0, ADDRESS2.Length - 4);
                            last4 = ADDRESS2.Substring(ADDRESS2.Length - 4);
                        }
                        int nPostcode = 0;
                        if (int.TryParse(last4, out nPostcode))
                        {
                            result.addressLine2 = $"{addrLast}";
                            result.postcode = $"{last4}";
                        }
                        else
                        {
                            result.addressLine2 = $"{ADDRESS2}";
                        }
                    }
                }
                else
                {
                    result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";

                    int lenAddrLast = ADDRESS3.Length;
                    string addrLast = "";
                    string last4 = "";
                    if (ADDRESS3.Length > 4)
                    {
                        addrLast = ADDRESS3.Substring(0, ADDRESS3.Length - 4);
                        last4 = ADDRESS3.Substring(ADDRESS3.Length - 4);
                    }
                    int nPostcode = 0;
                    if (int.TryParse(last4, out nPostcode))
                    {
                        result.addressLine2 = $"{addrLast}";
                        result.postcode = $"{last4}";
                    }
                    else
                    {
                        result.addressLine2 = $"{ADDRESS3}";
                    }
                }
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1} {ADDRESS2}";

                int lenAddrLast = ADDRESS4.Length;
                string addrLast = "";
                string last4 = "";
                if (ADDRESS4.Length > 4)
                {
                    addrLast = ADDRESS4.Substring(0, ADDRESS4.Length - 4);
                    last4 = ADDRESS4.Substring(ADDRESS4.Length - 4);
                }
                int nPostcode = 0;
                if (int.TryParse(last4, out nPostcode))
                {
                    result.addressLine2 = $"{ADDRESS3} {addrLast}";
                    result.postcode = $"{last4}";
                }
                else
                {
                    result.addressLine2 = $"{ADDRESS3} {ADDRESS4}";
                }
            }

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] ExtractFieldsFromReadResultOfPHUMID result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }
            return result;
        }
#if true
        public static ScanPHNIResult ExtractFieldsFromReadResultOfPHNI(IList<Line> linesAll)
        {
            LabelInfo labelREPUBLIKA_NG_PILIPINAS = new LabelInfo("REPUBLIKA NG PILIPINAS");
            LabelInfo labelRepublic_of_the_Philippines = new LabelInfo("Republic of the Philippines");
            LabelInfo labelPAMBANSANG_PAGKAKAKILANLAN = new LabelInfo("PAMBANSANG PAGKAKAKILANLAN");
            LabelInfo labelPhilippine_Identification_Card = new LabelInfo("Philippine Identification Card");
            Line linePCN = null;
            LabelInfo labelApelyido_Last_Name = new LabelInfo("Apelyido/Last Name");
            LabelInfo labelMga_Pangalan_Given_Names = new LabelInfo("Mga Pangalan/Given Names");
            LabelInfo labelGitnang_Apelyido_Middle_Name = new LabelInfo("Gitnang Apelyido/Middle Name");
            LabelInfo labelPetsa_ng_Kapanganakan_Date_of_Birth = new LabelInfo("Petsa ng Kapanganakan/Date of Birth");
            LabelInfo labelTirahan_Address = new LabelInfo("Tirahan/Address");

            ScanPHNIResult result = new ScanPHNIResult();

            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string ADDRESS1 = "";
            string ADDRESS2 = "";

            //var linesLeftOrder = linesAll.OrderBy(l => l.BoundingBox[0]);
            //double? leftEdgeOfBlock = linesAll.Min(l => l.BoundingBox[0]);
            //double? rightEdgeOfBlock = linesAll.Max(l => l.BoundingBox[2]);
            //double? topEdgeOfBlock = linesAll.Min(l => l.BoundingBox[1]);
            //double? bottomEdgeOfBlock = linesAll.Max(l => l.BoundingBox[5]);
            //double? sumLeft = linesLeftOrder.Take(4).Sum(l => l.BoundingBox[0]);
            //double? avgLeft = sumLeft / 4;
            //double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            //double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            //double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            //double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            // pick the lines aligned to left 
            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label
            //var linesLeftSide = linesAll.Where(l => l.BoundingBox[0] < avgLeft);
            //var linesNotLeftSide = linesAll.Where(l => l.BoundingBox[0] >= avgLeft);

            // find labels exactly match
            if (linesAll.Any())
            {
                // sort from top to bottom
                linesAll = linesAll.OrderBy(l => l.BoundingBox[1]).ToList();
                //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                int idxIdNum = -1;
                decimal heightIdNum = 0;
                //int numLines = linesLeftSide.Count();
                int numLines = linesAll.Count();
                //int idx = 0;
                //foreach (Line line in linesLeftSide)
                foreach (Line line in linesAll)
                {
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
#if false
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                    }
#else
                    heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        continue;
                    }

                    //"REPUBLIKA NG PILIPINAS"
                    if (!labelREPUBLIKA_NG_PILIPINAS.IsLabelFound)
                    {
                        if (labelREPUBLIKA_NG_PILIPINAS.MatchTitleExactly(line))
                            continue;
                    }
                    //"Republic of the Philippines"
                    if (!labelRepublic_of_the_Philippines.IsLabelFound)
                    {
                        if (labelRepublic_of_the_Philippines.MatchTitleExactly(line))
                            continue;
                    }
                    //"PAMBANSANG PAGKAKAKILANLAN"
                    if (!labelPAMBANSANG_PAGKAKAKILANLAN.IsLabelFound)
                    {
                        if (labelPAMBANSANG_PAGKAKAKILANLAN.MatchTitleExactly(line))
                            continue;
                    }
                    //"Philippine Identification Card"
                    if (!labelPhilippine_Identification_Card.IsLabelFound)
                    {
                        if (labelPhilippine_Identification_Card.MatchTitleExactly(line))
                            continue;
                    }

                    //"LASTNAME"
                    if (!labelApelyido_Last_Name.IsLabelFound)
                    {
                        if (labelApelyido_Last_Name.MatchTitleExactly(line))
                            continue;
                    }

                    //"GIVEN NAMES"
                    if (!labelMga_Pangalan_Given_Names.IsLabelFound)
                    {
                        if (labelMga_Pangalan_Given_Names.MatchTitleExactly(line))
                            continue;
                    }

                    //"MIDDLE NAME"
                    if (!labelGitnang_Apelyido_Middle_Name.IsLabelFound)
                    {
                        if (labelGitnang_Apelyido_Middle_Name.MatchTitleExactly(line))
                            continue;
                    }

                    //"DATE_OF_BIRTH"
                    if (!labelPetsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound)
                    {
                        if (labelPetsa_ng_Kapanganakan_Date_of_Birth.MatchTitleExactly(line))
                            continue;
                    }

                    //"Tirahan/Address"
                    if (!labelTirahan_Address.IsLabelFound)
                    {
                        if (labelTirahan_Address.MatchTitleExactly(line))
                            continue;
                    }

                    linesFieldOrLabel.Add(line);
                }
            }

            // find labels not found yet, and fields
            if (linesFieldOrLabel.Any())
            {
                // sort from top to bottom
                linesFieldOrLabel = linesFieldOrLabel.OrderBy(l => l.BoundingBox[1]).ToList();
                //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                //int idxIdNum = -1;
                decimal heightIdNum = 0;
                //int numLines = linesLeftSide.Count();
                int numLines = linesFieldOrLabel.Count();
                //foreach (Line line in linesLeftSide)
                foreach (Line line in linesFieldOrLabel)
                {
                    string text = line.Text.Trim();
                    decimal heightLine = 0;
#if false
                    if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                    }
                    else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                    {
                        heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                    }
#else
                    heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                    double? angle = line.ExtGetAngle();
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
                    if (angle == null || Math.Abs((decimal)angle) > 10)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                        continue;
                    }

                    if (string.IsNullOrEmpty(PCN))
                    {
                        //"REPUBLIKA NG PILIPINAS"
                        if (!labelREPUBLIKA_NG_PILIPINAS.IsLabelFound)
                        {
                            if (labelREPUBLIKA_NG_PILIPINAS.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Republic of the Philippines"
                        if (!labelRepublic_of_the_Philippines.IsLabelFound)
                        {
                            if (labelRepublic_of_the_Philippines.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"PAMBANSANG PAGKAKAKILANLAN"
                        if (!labelPAMBANSANG_PAGKAKAKILANLAN.IsLabelFound)
                        {
                            if (labelPAMBANSANG_PAGKAKAKILANLAN.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Philippine Identification Card"
                        if (!labelPhilippine_Identification_Card.IsLabelFound)
                        {
                            if (labelPhilippine_Identification_Card.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        try
                        {
                            //PCN
                            if (regexIDNum.Match(text).Success)
                            {
                                heightIdNum = heightLine;
                                PCN = text.Trim();
                                linePCN = line;
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        //"LASTNAME"
                        if (!labelApelyido_Last_Name.IsLabelFound)
                        {
                            if (labelApelyido_Last_Name.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(LAST_NAME))
                        {
                            if (!labelApelyido_Last_Name.IsLabelFound
                              || labelApelyido_Last_Name.IsFieldJustUnderTheLabel(line))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SURNAME");
                                LAST_NAME = line.Text;
                                continue;
                            }
                        }

                        //"GIVEN NAMES"
                        if (!labelMga_Pangalan_Given_Names.IsLabelFound)
                        {
                            if (labelMga_Pangalan_Given_Names.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(GIVEN_NAMES))
                        {
                            if (!labelMga_Pangalan_Given_Names.IsLabelFound
                                || labelMga_Pangalan_Given_Names.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> GIVEN_NAME");
                                GIVEN_NAMES = line.Text;
                                continue;
                            }
                        }

                        //"MIDDLE NAME"
                        if (!labelGitnang_Apelyido_Middle_Name.IsLabelFound)
                        {
                            if (labelGitnang_Apelyido_Middle_Name.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(MIDDLE_NAME))
                        {
                            // MIDDLE_NAME
                            if (!labelGitnang_Apelyido_Middle_Name.IsLabelFound
                                || labelGitnang_Apelyido_Middle_Name.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MIDDLE_NAME");
                                MIDDLE_NAME = line.Text;
                                continue;
                            }
                        }

                        //"DATE_OF_BIRTH"
                        if (!labelPetsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound)
                        {
                            if (labelPetsa_ng_Kapanganakan_Date_of_Birth.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        if (string.IsNullOrEmpty(DOB))
                        {
                            if (!labelPetsa_ng_Kapanganakan_Date_of_Birth.IsLabelFound
                                || labelPetsa_ng_Kapanganakan_Date_of_Birth.IsFieldJustUnderTheLabel(line)
                                )
                            {
                                try
                                {
                                    //(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER) \d{2}[.,] \d{4}
                                    Regex regexLine = new Regex("(JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER) \\d{2}[.,] \\d{4}");
                                    Match match = regexLine.Match(text);
                                    if (match.Success)
                                    {
                                        DOB = match.Value;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine(ex.Message);
                                }
                            }
                        }

                        //"Tirahan/Address"
                        if (!labelTirahan_Address.IsLabelFound)
                        {
                            if (labelTirahan_Address.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        if (string.IsNullOrEmpty(ADDRESS1))
                        {
                            if ((!labelTirahan_Address.IsLabelFound
                                || labelTirahan_Address.IsFieldJustUnderTheLabel(line))
                              && (linePCN != null && IsFieldInSameLeftEdgeOfLine(linePCN, line) && IsFieldsUnderTheLine(linePCN, line))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS1");
                                ADDRESS1 = line.Text;
                                //idxMainColumn++;
                                continue;
                            }
                        }
                        if (string.IsNullOrEmpty(ADDRESS2))
                        {
                            if ((!labelTirahan_Address.IsLabelFound
                                || labelTirahan_Address.IsFieldUnderTheLabel(line))
                              && (linePCN != null && IsFieldInSameLeftEdgeOfLine(linePCN, line) && IsFieldsUnderTheLine(linePCN, line))
                            )
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS2");
                                ADDRESS2 = line.Text;
                                //idxMainColumn++;
                                continue;
                            }
                        }
                    }
                    linesField.Add(line);
                }
            }

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // LAST_NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME))
            {
                lsMissingFields.Add("LAST_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME;
            }

            // GIVEN_NAMES -> firstName 
            if (string.IsNullOrEmpty(GIVEN_NAMES))
            {
                lsMissingFields.Add("GIVEN_NAMES");
            }
            else
            {
                result.firstName = GIVEN_NAMES;
            }

            // MIDDLE_NAME -> middleName 
            if (string.IsNullOrEmpty(MIDDLE_NAME))
            {
                lsMissingFields.Add("MIDDLE_NAME");
            }
            else
            {
                result.middleName = MIDDLE_NAME;
            }

            // PCN -> documentNumber
            if (string.IsNullOrEmpty(PCN))
            {
                lsMissingFields.Add("PCN");
            }
            else
            {
                result.documentNumber = PCN;
            }

            // (CITIZENSHIP) nationality is "PH" (by default)

            // DOB "yyyy/MM/dd" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";

                if (!string.IsNullOrEmpty(DOB))
                {
                    DateTime dtDoB;
                    if (DateTime.TryParse(DOB, out dtDoB))
                    {
                        int yyyy = dtDoB.Year;
                        int MM = dtDoB.Month;
                        int dd = dtDoB.Day;
                        result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    }
                    else
                    {
                        String[] token = DOB.Split(new char[] { ',', ' ' });
                        if (token.Length >= 3)
                        {
                            int MM = 0;
                            int dd = 0;
                            int yyyy = 0;
                            foreach (string v in token)
                            {
                                if (string.IsNullOrEmpty(v))
                                    continue;

                                if (MM == 0)
                                {
                                    MM = MonthNameToNum(v);
                                    continue;
                                }
                                if (dd == 0)
                                {
                                    dd = int.Parse(v);
                                    continue;
                                }
                                if (yyyy == 0)
                                {
                                    yyyy = int.Parse(v);
                                    continue;
                                }
                            }
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                        }
                    }
                }
                else
                {
                    lsMissingFields.Add("Date Of Birth");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("Date Of Birth");
            }

            // ADDRESS1, ADDRESS2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS1");
            }
            else
            {
                result.addressLine1 = $"{ADDRESS1}";
            }

            result.addressLine2 = $"{ADDRESS2}";

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHNI result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }
#endif
#if false
        //using ZXing
        static Result[] ReadQRCode(SkiaSharp.SKImage img)
        {
            SkiaSharp.SKBitmap bmp = SkiaSharp.SKBitmap.FromImage(img);
            SkiaSharp.SKSize skSize = bmp.Info.Size;
            if (skSize.Width > 500 || skSize.Height > 500)
            {
                int w = bmp.Info.Size.Width;
                int h = bmp.Info.Size.Height;
#if true
                int rate = 99;
                for (; rate > 0 && (w > 500 || h > 500); rate--)
                {
                    w = (int)Math.Round((bmp.Info.Size.Width * (rate / 100.0f)));
                    h = (int)Math.Round((bmp.Info.Size.Height * (rate / 100.0f)));
                }
#else
                    if (skSize.Width > skSize.Height)
                    {
                        w = 500;
                        h = (int)Math.Round(skSize.Height * (w / skSize.Width));
                    }
                    else
                    {
                        h = 500;
                        w = (int)Math.Round(skSize.Width * (h / skSize.Height));
                    }
#endif
                bmp = bmp.Resize(new SkiaSharp.SKSizeI(w, h), SkiaSharp.SKFilterQuality.High);
            }
#if DEBUG
            using (FileStream fs = new($"{DEBUG_OUTPUT_FOLDER}QR.png", FileMode.Create))
            {
                SkiaSharp.SKData dataImageQR = bmp.Encode(SkiaSharp.SKEncodedImageFormat.Png, 0);
                dataImageQR.SaveTo(fs);
            }
#endif
            ZXing.SkiaSharp.SKBitmapLuminanceSource skBmpLS = new ZXing.SkiaSharp.SKBitmapLuminanceSource(bmp);
            ZXing.Common.HybridBinarizer hybridBinarizer = new ZXing.Common.HybridBinarizer(skBmpLS);
            ZXing.BinaryBitmap bb = new ZXing.BinaryBitmap(hybridBinarizer);

            //ZXing.QrCode.QRCodeReader qrCodeRdr = new QRCodeReader();
            //Result res = qrCodeRdr.decode(bb);
            //if (res != null)
            //{
            //    Console.WriteLine(res.ToString());
            //}
            Newtonsoft.Json.Linq.JObject? jsonDataInQRCode = null;
            ZXing.SkiaSharp.BarcodeReader rdr = new ZXing.SkiaSharp.BarcodeReader();
            Result[] resMulti = rdr.DecodeMultiple(bmp);
            return resMulti;
        }
#endif
        public static ScanPHNIBKResult ExtractFieldsFromReadResultOfPHNIBK(IList<Line> linesAll, /*PredictionModel? predictionPHNI_BK_QR,*/ SKBitmap bitmapRotated)
        {
            ScanPHNIBKResult result = new ScanPHNIBKResult();

            // Fields
            string DATE_OF_ISSUE = "";
            string PCN = "";
            string LAST_NAME = "";
            string GIVEN_NAMES = "";
            string MIDDLE_NAME = "";
            string DOB = "";
            string POB = "";
            string SEX = "";
            string BLOOD_TYPE = "";
            string MARITAL_STATUS = "";

            // Labels
            LabelInfo labelAraw_ng_pagkakaloob_Date_of_issue = new LabelInfo("Araw ng pagkakaloob/Date of issue");
            LabelInfo labelKasarian_Sex = new LabelInfo("Kasarian/Sex");
            Line valueKasarian_Sex = null;
            LabelInfo labelUri_ng_Dugo_Blood_Type = new LabelInfo("Uri ng Dugo/Blood Type");
            LabelInfo labelKalagayang_Sibil_Marital_Status = new LabelInfo("Kalagayang Sibil/Marital Status");
            //Line? valueKalagayang_Sibil_Marital_Status = null;
            LabelInfo labelLugar_ng_Kapanganakan_Place_of_Birth = new LabelInfo("Lugar ng Kapanganakan/Place of Birth");

            // Fields extracted from QR code
            string qrcode_DateIssued = "";
            string qrcode_Issuer = "";
            string qrcode_alg = "";
            string qrcode_signature = "";
            JObject qrcode_subject = null;
            string qrcode_subject_Suffix = "";
            string qrcode_subject_lName = "";
            string qrcode_subject_fName = "";
            string qrcode_subject_mName = "";
            string qrcode_subject_sex = "";
            string qrcode_subject_BT = "";
            string qrcode_subject_DOB = "";
            string qrcode_subject_POB = "";
            string qrcode_subject_PCN = "";

#if false
            // read QR code
            if (predictionPHNI_BK_QR != null)
            {
                const float margin = 0.075f;
                BoundingBox box = predictionPHNI_BK_QR.BoundingBox;
                SKImage img = SKImage.FromBitmap(bitmapRotated);
                // read QR code from original image
                Result[] resMulti = ReadQRCode(img);
                if (resMulti == null)
                {
                    // crop QR code and try again...
                    int left = (box.Left < margin) ? 0 : (int)Math.Round((box.Left - margin) * img.Width);
                    int top = (box.Top < margin) ? 0 : (int)Math.Round((box.Top - margin) * img.Height);
                    int right = ((box.Left + box.Width + margin) > 1.0) ? img.Width : (int)Math.Round((box.Left + box.Width + margin) * img.Width);
                    int bottom = ((box.Top + box.Height + margin) > 1.0) ? img.Height : (int)Math.Round((box.Top + box.Height + margin) * img.Height);
                    SKRectI skRectQR = new SKRectI(left, top, right, bottom);
                    img = img.Subset(skRectQR);
                    resMulti = ReadQRCode(img);
                }

                if (resMulti != null)
                {
                    foreach (Result aResult in resMulti)
                    {
                        Console.WriteLine(aResult.Text);
                        try
                        {
                            Newtonsoft.Json.Linq.JObject jsonObject = Newtonsoft.Json.Linq.JObject.Parse(aResult.Text);
                            if (jsonObject != null)
                            {
                                qrcode_DateIssued = (string)jsonObject.GetValue("DateIssued");
                                qrcode_Issuer = (string)jsonObject.GetValue("Issuer");
                                qrcode_alg = (string)jsonObject.GetValue("alg");
                                qrcode_signature = (string)jsonObject.GetValue("signature");
                                qrcode_subject = (JObject)jsonObject.GetValue("subject");
                                if (qrcode_subject != null)
                                {
                                    qrcode_subject_Suffix = (string)qrcode_subject.GetValue("Suffix");
                                    qrcode_subject_lName = (string)qrcode_subject.GetValue("lName");
                                    qrcode_subject_fName = (string)qrcode_subject.GetValue("fName");
                                    qrcode_subject_mName = (string)qrcode_subject.GetValue("mName");
                                    qrcode_subject_sex = (string)qrcode_subject.GetValue("sex");
                                    qrcode_subject_BT = (string)qrcode_subject.GetValue("BF");
                                    qrcode_subject_DOB = (string)qrcode_subject.GetValue("DOB");
                                    qrcode_subject_POB = (string)qrcode_subject.GetValue("POB");
                                    qrcode_subject_PCN = (string)qrcode_subject.GetValue("PCN");

                                    result.IsQRCodeDataValid = true;
                                    result.QRCodeData = aResult.Text;
                                }
                                Console.WriteLine($"DateIssued: {qrcode_DateIssued}");
                                Console.WriteLine($"Issuer: {qrcode_Issuer}");
                                Console.WriteLine($"alg: {qrcode_alg}");
                                Console.WriteLine($"signature: {qrcode_signature}");
                                Console.WriteLine($"subject:");
                                Console.WriteLine($"  Suffix: {qrcode_subject_Suffix}");
                                Console.WriteLine($"  lName: {qrcode_subject_lName}");
                                Console.WriteLine($"  fName: {qrcode_subject_fName}");
                                Console.WriteLine($"  mName: {qrcode_subject_mName}");
                                Console.WriteLine($"  sex: {qrcode_subject_sex}");
                                Console.WriteLine($"  BT: {qrcode_subject_BT}");
                                Console.WriteLine($"  DOB: {qrcode_subject_DOB}");
                                Console.WriteLine($"  POB: {qrcode_subject_POB}");
                                Console.WriteLine($"  PCN: {qrcode_subject_PCN}");

                                if (result.IsQRCodeDataValid)
                                {
                                    DATE_OF_ISSUE = qrcode_DateIssued;
                                    confidence_DATE_OF_ISSUE = new Confidence(1);
                                    LAST_NAME = qrcode_subject_lName;
                                    confidence_LAST_NAME = new Confidence(1);
                                    GIVEN_NAMES = qrcode_subject_fName;
                                    confidence_GIVEN_NAMES = new Confidence(1);
                                    MIDDLE_NAME = qrcode_subject_mName;
                                    confidence_MIDDLE_NAME = new Confidence(1);
                                    SEX = qrcode_subject_sex;
                                    confidence_SEX = new Confidence(1);
                                    DOB = qrcode_subject_DOB;
                                    confidence_DOB = new Confidence(1);
                                    POB = qrcode_subject_POB;
                                    confidence_POB = new Confidence(1);
                                    PCN = qrcode_subject_PCN;
                                    confidence_PCN = new Confidence(1);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {ex}");
                        }
                    }
                    /*
{
"DateIssued": "12 September 2022",
"Issuer": "PSA",
"subject": {
    "Suffix": "",
    "lName": "DELOS REYES",
    "fName": "CHRISTIAN MARX",
    "mName": "LOZADA",
    "sex": "Male",
    "BF": "[1,9]",
    "DOB": "June 06, 1988",
    "POB": "City of Caloocan,NCR, THIRD DISTRICT",
    "PCN": "5931-9426-7546-1037"
},
"alg": "EDDSA",
"signature": "H6WF1LJOcXPiMlE6VTBgamixsA8GqxJ3tJpxpSDmR9qoCMj4/jBKJo3PyP3PdtmMBwXa/ZlypuIOkkcZxqzrAw=="
}
                    */
                }
            }
#endif
            // pick the lines aligned to left 
            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            if (result.IsQRCodeDataValid)
            {
                // need to read only MARITAL_STATUS from image
                // find labels exactly match
                if (linesAll.Any())
                {
                    // sort from top to bottom
                    linesAll = linesAll.OrderBy(l => l.BoundingBox[1]).ToList();
                    //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                    System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                    Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                    int idxIdNum = -1;
                    decimal heightIdNum = 0;
                    //int numLines = linesLeftSide.Count();
                    int numLines = linesAll.Count();
                    //int idx = 0;
                    //foreach (Line line in linesLeftSide)
                    foreach (Line line in linesAll)
                    {
                        string text = line.Text.Trim();
                        double? angle = line.ExtGetAngle();
                        if (angle == null || Math.Abs((decimal)angle) > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                            continue;
                        }

                        //"Kalagayang Sibil/Marital Status"
                        if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound)
                        {
                            if (labelKalagayang_Sibil_Marital_Status.MatchTitleExactly(line))
                                continue;

                            if (labelKalagayang_Sibil_Marital_Status.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        // Marital Status
                        if (string.IsNullOrEmpty(MARITAL_STATUS))
                        {
                            //"Kalagayang Sibil/Marital Status"
                            if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound)
                            {
                                if (CheckCharInLine(line, "SINGLE")
                                 || CheckCharInLine(line, "MARRIED")
                                 || CheckCharInLine(line, "SEPARATED")
                                 || CheckCharInLine(line, "DIVORCED")
                                 || CheckCharInLine(line, "WIDOW")
                                 || CheckCharInLine(line, "WIDOWER")
                                 || CheckCharInLine(line, "WIDOWED"))   // Widow/er
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                                    MARITAL_STATUS = line.Text;
                                    break;
                                }
                            }
                            else if (labelKalagayang_Sibil_Marital_Status.IsFieldJustUnderTheLabel(line))
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                                MARITAL_STATUS = line.Text;
                                break;
                            }
                        }

                        //linesFieldOrLabel.Add(line);
                    }
                }

                // sort from top to bottom
                linesFieldOrLabel.OrderBy(l => l.BoundingBox[1]);

                foreach (Line line in linesFieldOrLabel)
                {
                    string text = line.Text.Trim();

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()}");

                    // Marital Status
                    if (string.IsNullOrEmpty(MARITAL_STATUS))
                    {
                        //"Kalagayang Sibil/Marital Status"
                        if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound)
                        {
                            if (CheckCharInLine(line, "SINGLE")
                             || CheckCharInLine(line, "MARRIED")
                             || CheckCharInLine(line, "SEPARATED")
                             || CheckCharInLine(line, "DIVORCED")
                             || CheckCharInLine(line, "WIDOW")
                             || CheckCharInLine(line, "WIDOWER")
                             || CheckCharInLine(line, "WIDOWED"))   // Widow/er
                            {
                                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                                MARITAL_STATUS = line.Text;
                            }
                            break;
                        }
                        else if (labelKalagayang_Sibil_Marital_Status.IsFieldJustUnderTheLabel(line))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                            MARITAL_STATUS = line.Text;
                            break;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ignore...");
                }// foreach lines in main column
            }
            else
            {
                // find labels exactly match
                if (linesAll.Any())
                {
                    // sort from top to bottom
                    linesAll = linesAll.OrderBy(l => l.BoundingBox[1]).ToList();
                    //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                    System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                    Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                    int idxIdNum = -1;
                    decimal heightIdNum = 0;
                    //int numLines = linesLeftSide.Count();
                    int numLines = linesAll.Count();
                    //int idx = 0;
                    //foreach (Line line in linesLeftSide)
                    foreach (Line line in linesAll)
                    {
                        string text = line.Text.Trim();
                        decimal heightLine = 0;
#if false
                        if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                        {
                            heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                        }
                        else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                        {
                            heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                        }
#else
                        heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                        double? angle = line.ExtGetAngle();
                        if (angle == null || Math.Abs((decimal)angle) > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                            continue;
                        }

                        //"Araw ng pagkakaloob/Date of issue"
                        if (!labelAraw_ng_pagkakaloob_Date_of_issue.IsLabelFound)
                        {
                            if (labelAraw_ng_pagkakaloob_Date_of_issue.MatchTitleExactly(line))
                                continue;
                        }
                        //"Kasarian/Sex"
                        if (!labelKasarian_Sex.IsLabelFound)
                        {
                            if (labelKasarian_Sex.MatchTitleExactly(line))
                                continue;
                        }
                        //"Uri ng Dugo/Blood Type"
                        if (!labelUri_ng_Dugo_Blood_Type.IsLabelFound)
                        {
                            if (labelUri_ng_Dugo_Blood_Type.MatchTitleExactly(line))
                                continue;
                        }
                        //"Kalagayang Sibil/Marital Status"
                        if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound)
                        {
                            if (labelKalagayang_Sibil_Marital_Status.MatchTitleExactly(line))
                                continue;
                        }
                        //"Lugar ng Kapanganakan/Place of Birth"
                        if (!labelLugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound)
                        {
                            if (labelLugar_ng_Kapanganakan_Place_of_Birth.MatchTitleExactly(line))
                                continue;
                        }

                        linesFieldOrLabel.Add(line);
                    }
                }

                // find labels not found yet, and fields
                if (linesFieldOrLabel.Any())
                {
                    // sort from top to bottom
                    linesFieldOrLabel = linesFieldOrLabel.OrderBy(l => l.BoundingBox[1]).ToList();
                    //linesLeftSide = linesLeftSide.OrderBy(l => l.BoundingBox[1]);
                    System.Diagnostics.Debug.WriteLine("\nLines aligned to left:");
                    Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{4}-\d{4}");
                    //int idxIdNum = -1;
                    decimal heightIdNum = 0;
                    //int numLines = linesLeftSide.Count();
                    int numLines = linesFieldOrLabel.Count();
                    //foreach (Line line in linesLeftSide)
                    foreach (Line line in linesFieldOrLabel)
                    {
                        string text = line.Text.Trim();
                        decimal heightLine = 0;
#if false
                        if (line.BoundingBox.Count == 8 && line.BoundingBox[7].HasValue && line.BoundingBox[1].HasValue)
                        {
                            heightLine = Math.Abs((decimal)line.BoundingBox[7] - (decimal)line.BoundingBox[1]);
                        }
                        else if (line.BoundingBox.Count == 4 && line.BoundingBox[1].HasValue && line.BoundingBox[3].HasValue)
                        {
                            heightLine = Math.Abs((decimal)line.BoundingBox[3] - (decimal)line.BoundingBox[1]);
                        }
#else
                        heightLine = Math.Abs((decimal)line.ExtGetHeight());
#endif

                        double? angle = line.ExtGetAngle();
                        if (angle == null || Math.Abs((decimal)angle) > 10)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                            continue;
                        }

                        //"Araw ng pagkakaloob/Date of issue"
                        if (!labelAraw_ng_pagkakaloob_Date_of_issue.IsLabelFound)
                        {
                            if (labelAraw_ng_pagkakaloob_Date_of_issue.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Kasarian/Sex"
                        if (!labelKasarian_Sex.IsLabelFound)
                        {
                            if (labelKasarian_Sex.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Uri ng Dugo/Blood Type"
                        if (!labelUri_ng_Dugo_Blood_Type.IsLabelFound)
                        {
                            if (labelUri_ng_Dugo_Blood_Type.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Kalagayang Sibil/Marital Status"
                        if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound)
                        {
                            if (labelKalagayang_Sibil_Marital_Status.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                        //"Lugar ng Kapanganakan/Place of Birth"
                        if (!labelLugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound)
                        {
                            if (labelLugar_ng_Kapanganakan_Place_of_Birth.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }

                        linesField.Add(line);
                    }
                }

                // sort from top to bottom
                //linesField.OrderBy(l => l.BoundingBox[1]);
                linesField.OrderBy(l => l.ExtGetTop());

                foreach (Line line in linesField)
                {
                    string text = line.Text.Trim();

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()}");

                    if (string.IsNullOrEmpty(DATE_OF_ISSUE))
                    {
                        if (!labelAraw_ng_pagkakaloob_Date_of_issue.IsLabelFound
                            || labelAraw_ng_pagkakaloob_Date_of_issue.IsFieldJustUnderTheLabel(line)
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> DATE_OF_ISSUE");
                            DATE_OF_ISSUE = line.Text;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(SEX) && valueKasarian_Sex == null)
                    {
                        //"Kasarian/Sex"
                        if (!labelKasarian_Sex.IsLabelFound
                          || labelKasarian_Sex.IsFieldJustUnderTheLabel(line))
                        {
                            valueKasarian_Sex = line;
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(BLOOD_TYPE))
                    {
                        //"Uri ng Dugo/Blood Type"
                        if (!labelUri_ng_Dugo_Blood_Type.IsLabelFound
                            || labelUri_ng_Dugo_Blood_Type.IsFieldJustUnderTheLabel(line)
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> BLOOD_TYPE");
                            BLOOD_TYPE = line.Text;
                            //idxMainColumn++;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(MARITAL_STATUS))
                    {
                        //"Kalagayang Sibil/Marital Status"
                        if (!labelKalagayang_Sibil_Marital_Status.IsLabelFound
                            || labelKalagayang_Sibil_Marital_Status.IsFieldJustUnderTheLabel(line)
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> MARITAL_STATUS");
                            MARITAL_STATUS = line.Text;
                            //idxMainColumn++;
                            continue;
                        }
                    }

                    if (string.IsNullOrEmpty(POB))
                    {
                        //"Lugar ng Kapanganakan/Place of Birth"
                        if (!labelLugar_ng_Kapanganakan_Place_of_Birth.IsLabelFound
                            || labelLugar_ng_Kapanganakan_Place_of_Birth.IsFieldJustUnderTheLabel(line)
                            )
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> POB");
                            POB = line.Text;
                            //idxMainColumn++;
                            continue;
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                    //idxMainColumn++;
                }// foreach lines in main column
            }

            // map to result and convert format 

            // LAST_NAME -> lastNameOrFullName 
            result.lastNameOrFullName = LAST_NAME;

            // GIVEN_NAMES -> firstName 
            result.firstName = GIVEN_NAMES;

            // MIDDLE_NAME -> middleName 
            result.middleName = MIDDLE_NAME;

            // IDNUM -> documentNumber
            result.documentNumber = PCN;

            // POB -> placeOfBirth
            result.placeOfBirth = POB;

            // DATE_OF_ISSUE "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.documentIssueDate = ConvertDateString(DATE_OF_ISSUE);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // DOB "MMM dd, yyyy" -> dateOfBirth "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = ConvertDateString(DOB);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            // Gender
            if (valueKasarian_Sex != null)
            {
                if (CheckCharInLine(valueKasarian_Sex, "MALE"))
                {
                    result.gender = "M";
                }
                else if (CheckCharInLine(valueKasarian_Sex, "FEMALE"))
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = valueKasarian_Sex.Text.Trim();
                }
            }
            else
            {
                if (SEX.ToUpper() == "MALE")
                {
                    result.gender = "M";
                }
                else if (SEX.ToLower() == "FEMALE")
                {
                    result.gender = "F";
                }
                else
                {
                    // unknown...
                    result.gender = SEX.Trim();
                }
            }

            // Marital Status
            /*
            Civil Status:		
            S	Single	
            M	Married	
            X	Separated/Divorced
            W	Widow/er
            */
            switch (MARITAL_STATUS.ToUpper())
            {
                case "SINGLE":
                    result.maritalStatus = "S";
                    break;
                case "MARRIED":
                    result.maritalStatus = "M";
                    break;
                case "SEPARATED":
                    result.maritalStatus = "X";
                    break;
                case "DIVORCED":
                    result.maritalStatus = "X";
                    break;
                case "WIDOW":
                    result.maritalStatus = "W";
                    break;
                case "WIDOWER":
                    result.maritalStatus = "W";
                    break;
                default:
                    result.maritalStatus = MARITAL_STATUS.Trim();   // Unknown
                    break;
            }

            result.Success = true;
            return result;
        }

        static string ConvertDateString(string strDate)
        {
            string ret = "";
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dtDoB;
                if (DateTime.TryParse(strDate, out dtDoB))
                {
                    int yyyy = dtDoB.Year;
                    int MM = dtDoB.Month;
                    int dd = dtDoB.Day;
                    ret = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    String[] token = strDate.Split(new char[] { ',', ' ' });
                    if (token.Length >= 3)
                    {
                        int MM = 0;
                        int dd = 0;
                        int yyyy = 0;
                        foreach (string v in token)
                        {
                            if (string.IsNullOrEmpty(v))
                                continue;

                            if (MM == 0)
                            {
                                MM = MonthNameToNum(v);
                                continue;
                            }
                            if (dd == 0)
                            {
                                dd = int.Parse(v);
                                continue;
                            }
                            if (yyyy == 0)
                            {
                                yyyy = int.Parse(v);
                                continue;
                            }
                        }
                        ret = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    }
                }
            }
            return ret;
        }

        static int MonthNameToNum(string val)
        {
            if (string.IsNullOrEmpty(val))
                return 0;
            //JANUARY|FEBRUARY|MARCH|APRIL|MAY|JUNE|JULY|AUGUST|SEPTEMBER|OCTOBER|NOVEMBER|DECEMBER
            switch (val.ToUpper())
            {
                case "JAN":
                case "JANUARY":
                    return 1;
                case "FEB":
                case "FEBRUARY":
                    return 2;
                case "MAR":
                case "MARCH":
                    return 3;
                case "APR":
                case "APRIL":
                    return 4;
                case "MAY":
                    return 5;
                case "JUN":
                case "JUNE":
                    return 6;
                case "JUL":
                case "JULY":
                    return 7;
                case "AUG":
                case "AUGUST":
                    return 8;
                case "SEP":
                case "SEPTEMBER":
                    return 9;
                case "OCT":
                case "OCTOBER":
                    return 10;
                case "NOV":
                case "NOVEMBER":
                    return 11;
                case "DEC":
                case "DECEMBER":
                    return 12;
                default:
                    return 0;
            }
        }
        public static ScanPHDLResult ExtractFieldsFromReadResultOfPHDL(IList<Line> linesAll)
        {
            char[] SEPARATOR_NAME = new char[] { ',', '.', ' ' };

            LabelInfo labelREPUBLIC_OF_THE_PHILIPPINES = new LabelInfo("REPUBLIC OF THE PHILIPPINES");
            LabelInfo labelDEPARTMENT_OF_TRANSPORTATION = new LabelInfo("DEPARTMENT OF TRANSPORTATION");
            LabelInfo labelLAND_TRANSPORTATION_OFFICE = new LabelInfo("LAND TRANSPORTATION OFFICE");
            LabelInfo labelNON_PROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("NON-PROFESSIONAL DRIVER'S LICENSE");
            LabelInfo labelPROFESSIONAL_DRIVERS_LICENSE = new LabelInfo("PROFESSIONAL DRIVER'S LICENSE");
            LabelInfo labelDRIVERS_LICENSE = new LabelInfo("DRIVER'S LICENSE");
            LabelInfo labelLast_Name_First_Name_Middle_Name = new LabelInfo("Last Name, First Name, Middle Name");
            LabelInfo labelNationality = new LabelInfo("Nationality");
            LabelInfo labelSex = new LabelInfo("Sex");
            LabelInfo labelDateOfBirth = new LabelInfo("Date Of Birth");
            LabelInfo labelWeight_kg_Height_m = new LabelInfo("Weight (kg) Height(m)");
            LabelInfo labelWeight_kg = new LabelInfo("Weight (kg)");
            LabelInfo labelHeight_m = new LabelInfo("Height(m)");
            LabelInfo labelAddress = new LabelInfo("Address");
            LabelInfo labelLicense_No = new LabelInfo("License No.");
            LabelInfo labelExpiration_Date = new LabelInfo("Expiration Date");
            LabelInfo labelAgency_Code = new LabelInfo("Agency Code");
            LabelInfo labelBlood_Type = new LabelInfo("Blood Type");
            LabelInfo labelEyes_Color = new LabelInfo("Eyes Color");
            LabelInfo labelRestrictions = new LabelInfo("Restrictions");
            LabelInfo labelConditions = new LabelInfo("Conditions");

            ScanPHDLResult result = new ScanPHDLResult();

            string LAST_NAME_FISRT_MIDDLE_NAME = "";
            string LAST_NAME = "";
            string FISRT_MIDDLE_NAME = "";
            string NATIONALITY_SEX_DOB = "";
            string NATIONALITY = "";
            string SEX = "";
            string DOB = "";
            string ADDRESS_1 = "";
            Line lineAddress1 = null;
            string ADDRESS_2 = "";
            string ADDRESS = "";
            string LICENSE_NO_EXPIRY = "";
            string LICENSE_NO = "";
            string EXPIRY = "";

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // fid labels exactly match
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()}");

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelREPUBLIC_OF_THE_PHILIPPINES.IsLabelFound)
                {
                    if (labelREPUBLIC_OF_THE_PHILIPPINES.MatchTitleExactly(line))
                        continue;
                }
                if (!labelDEPARTMENT_OF_TRANSPORTATION.IsLabelFound)
                {
                    if (labelDEPARTMENT_OF_TRANSPORTATION.MatchTitleExactly(line))
                        continue;
                }
                if (!labelLAND_TRANSPORTATION_OFFICE.IsLabelFound)
                {
                    if (labelLAND_TRANSPORTATION_OFFICE.MatchTitleExactly(line))
                        continue;
                }
                if (!labelNON_PROFESSIONAL_DRIVERS_LICENSE.IsLabelFound)
                {
                    if (labelNON_PROFESSIONAL_DRIVERS_LICENSE.MatchTitleExactly(line))
                        continue;

                    if (!labelPROFESSIONAL_DRIVERS_LICENSE.IsLabelFound)
                    {
                        if (labelPROFESSIONAL_DRIVERS_LICENSE.MatchTitleExactly(line))
                            continue;

                        if (!labelDRIVERS_LICENSE.IsLabelFound)
                        {
                            if (labelDRIVERS_LICENSE.MatchTitleExactly(line))
                                continue;
                        }
                    }
                }
                if (!labelLast_Name_First_Name_Middle_Name.IsLabelFound)
                {
                    if (labelLast_Name_First_Name_Middle_Name.MatchTitleExactly(line))
                        continue;
                }
                if (!labelNationality.IsLabelFound)
                {
                    if (labelNationality.MatchTitleExactly(line))
                        continue;
                }
                if (!labelSex.IsLabelFound)
                {
                    if (labelSex.MatchTitleExactly(line))
                        continue;
                }
                if (!labelDateOfBirth.IsLabelFound)
                {
                    if (labelDateOfBirth.MatchTitleExactly(line))
                        continue;
                }
                if (!labelWeight_kg_Height_m.IsLabelFound)
                {
                    if (labelWeight_kg_Height_m.MatchTitleExactly(line))
                        continue;

                    if (!labelWeight_kg.IsLabelFound)
                    {
                        if (labelWeight_kg.MatchTitleExactly(line))
                            continue;

                        if (!labelHeight_m.IsLabelFound)
                        {
                            if (labelHeight_m.MatchTitleExactly(line))
                                continue;
                        }
                    }
                }
                if (!labelAddress.IsLabelFound)
                {
                    if (labelAddress.MatchTitleExactly(line))
                        continue;
                }
                if (!labelLicense_No.IsLabelFound)
                {
                    if (labelLicense_No.MatchTitleExactly(line))
                        continue;
                }
                if (!labelExpiration_Date.IsLabelFound)
                {
                    if (labelExpiration_Date.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAgency_Code.IsLabelFound)
                {
                    if (labelAgency_Code.MatchTitleExactly(line))
                        continue;
                }
                if (!labelBlood_Type.IsLabelFound)
                {
                    if (labelBlood_Type.MatchTitleExactly(line))
                        continue;
                }
                if (!labelEyes_Color.IsLabelFound)
                {
                    if (labelEyes_Color.MatchTitleExactly(line))
                        continue;
                }
                if (!labelRestrictions.IsLabelFound)
                {
                    if (labelRestrictions.MatchTitleExactly(line))
                        continue;
                }
                if (!labelConditions.IsLabelFound)
                {
                    if (labelConditions.MatchTitleExactly(line))
                        continue;
                }

                linesFieldOrLabel.Add(line);
            }// foreach lines in other columns

            // find labels not found yet, or fields
            foreach (Line line in linesFieldOrLabel)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()} ");

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelREPUBLIC_OF_THE_PHILIPPINES.IsLabelFound)
                {
                    if (labelREPUBLIC_OF_THE_PHILIPPINES.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelDEPARTMENT_OF_TRANSPORTATION.IsLabelFound)
                {
                    if (labelDEPARTMENT_OF_TRANSPORTATION.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelLAND_TRANSPORTATION_OFFICE.IsLabelFound)
                {
                    if (labelLAND_TRANSPORTATION_OFFICE.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelNON_PROFESSIONAL_DRIVERS_LICENSE.IsLabelFound)
                {
                    if (labelNON_PROFESSIONAL_DRIVERS_LICENSE.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;

                    if (!labelPROFESSIONAL_DRIVERS_LICENSE.IsLabelFound)
                    {
                        if (labelPROFESSIONAL_DRIVERS_LICENSE.MatchTitle(line/*, mSpellSuggestion*/))
                            continue;

                        if (!labelDRIVERS_LICENSE.IsLabelFound)
                        {
                            if (labelDRIVERS_LICENSE.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                    }
                }
                if (!labelLast_Name_First_Name_Middle_Name.IsLabelFound)
                {
                    if (labelLast_Name_First_Name_Middle_Name.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelNationality.IsLabelFound)
                {
                    if (labelNationality.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelSex.IsLabelFound)
                {
                    if (labelSex.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelDateOfBirth.IsLabelFound)
                {
                    if (labelDateOfBirth.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelWeight_kg_Height_m.IsLabelFound)
                {
                    if (labelWeight_kg_Height_m.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;

                    if (!labelWeight_kg.IsLabelFound)
                    {
                        if (labelWeight_kg.MatchTitle(line/*, mSpellSuggestion*/))
                            continue;

                        if (!labelHeight_m.IsLabelFound)
                        {
                            if (labelHeight_m.MatchTitle(line/*, mSpellSuggestion*/))
                                continue;
                        }
                    }
                }
                if (!labelAddress.IsLabelFound)
                {
                    if (labelAddress.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelLicense_No.IsLabelFound)
                {
                    if (labelLicense_No.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelExpiration_Date.IsLabelFound)
                {
                    if (labelExpiration_Date.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelAgency_Code.IsLabelFound)
                {
                    if (labelAgency_Code.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelBlood_Type.IsLabelFound)
                {
                    if (labelBlood_Type.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelEyes_Color.IsLabelFound)
                {
                    if (labelEyes_Color.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelRestrictions.IsLabelFound)
                {
                    if (labelRestrictions.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelConditions.IsLabelFound)
                {
                    if (labelConditions.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }

                linesField.Add(line);
            }// foreach lines in other columns

            //var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);
            var linesLeftOrder = linesField.OrderBy(l => l.ExtGetLeft());
            int countLinesField = linesField.Count;
            int idxMedianLinesField = countLinesField / 2;
            //double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
            double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];
            //double? leftMiddleOfFields = (probabilityMYDL_Flag > 0) ? leftMYDL_Flag : leftMedian;
#if false
            double? leftEdgeOfBlock = linesField.Min(l => l.BoundingBox[0]);
            double? rightEdgeOfBlock = linesField.Max(l => l.BoundingBox[2]);
            double? topEdgeOfBlock = linesField.Min(l => l.BoundingBox[1]);
            double? bottomEdgeOfBlock = linesField.Max(l => l.BoundingBox[5]);
            double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
#else
            double? leftEdgeOfBlock = linesField.Min(l => l.ExtGetLeft());
            double? rightEdgeOfBlock = linesField.Max(l => l.ExtGetRight());
            double? topEdgeOfBlock = linesField.Min(l => l.ExtGetTop());
            double? bottomEdgeOfBlock = linesField.Max(l => l.ExtGetBottom());
            double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.ExtGetLeft());
#endif
            double? avgLeft = sumLeft / 5;
            double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            //var linesInMainColumn = linesField.Where(l => (decimal)l.BoundingBox[0] <= (decimal)leftMiddleOfFields);
            //var linesOutOfMainColumn = linesField.Where(l => (decimal)l.BoundingBox[0] > (decimal)leftMiddleOfFields);

            double? heightName = null;
            double? bottomName = null;
            //int numLinesInMainColumn = linesInMainColumn.Count();
            int numLinesField = linesField.Count;
            int idxMainColumn = 0;
            // sort from top to bottom
            //linesInMainColumn.OrderBy(l => l.BoundingBox[1]);

            //foreach (Line line in linesInMainColumn)
            foreach (Line line in linesField)
            {
                string text = line.Text.Trim();
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()} ");

                if (heightName.HasValue)
                {
                    if (line.ExtGetHeight() < heightName * 0.5)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                        //numLinesInMainColumn--;
                        numLinesField--;
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesField:{numLinesField}");
                /*
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesInMainColumn:{numLinesInMainColumn}");
                //if (idxMainColumn + 1 == numLinesInMainColumn)
                if (idxMainColumn + 1 == numLinesField)
                {
                    // STATE
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> STATE");
                    STATE = line.Text;
                    confidence_STATE = confidence;
                    idxMainColumn++;
                    continue;
                }
                //if (idxMainColumn + 2 == numLinesInMainColumn)
                if (idxMainColumn + 2 == numLinesField)
                {
                    // POSTCODE CITY
                    string postcode_city = text;
                    string[] token = postcode_city.Split(' ', 2);
                    if (token.Length > 1)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> POSTCODE: {token[0]} CITY: {token[1]}");
                        POSTCODE = token[0];
                        confidence_POSTCODE = new Confidence(line.ExtGetConfidenceArray());
                        CITY = token[1];
                        confidence_CITY = new Confidence(line.ExtGetConfidenceArray());
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> city_postcode: {postcode_city}");
                    }
                    idxMainColumn++;
                    continue;
                }
                */
                if (string.IsNullOrEmpty(LAST_NAME_FISRT_MIDDLE_NAME))
                {
                    // Last Name, FirstName Middle Name
#if false
                    if (!labelLast_Name_First_Name_Middle_Name.IsLabelFound
                        ||
                        ((double)(line.BoundingBox[1].Value - labelLast_Name_First_Name_Middle_Name.Bottom) >= 0
                        && (double)(line.BoundingBox[1].Value - labelLast_Name_First_Name_Middle_Name.Bottom) < labelLast_Name_First_Name_Middle_Name.Height * 4
                        && labelLast_Name_First_Name_Middle_Name.Height < line.ExtGetHeight())
                        )
#else
                    if (!labelLast_Name_First_Name_Middle_Name.IsLabelFound
                        ||
                        ((double)(line.ExtGetTop().Value - labelLast_Name_First_Name_Middle_Name.Bottom) >= 0
                        && (double)(line.ExtGetTop().Value - labelLast_Name_First_Name_Middle_Name.Bottom) < labelLast_Name_First_Name_Middle_Name.Height * 4
                        && labelLast_Name_First_Name_Middle_Name.Height < line.ExtGetHeight())
                        )
#endif
                    {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LAST_NAME_FISRT_MIDDLE_NAME");
                        LAST_NAME_FISRT_MIDDLE_NAME = line.Text;
                        heightName = line.ExtGetHeight();
                        bottomName = line.ExtGetBottom();
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(NATIONALITY))
                {
                    // Nationality
                    if (!labelNationality.IsLabelFound
                        || labelNationality.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> NATIONALITY");
                        NATIONALITY = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(SEX))
                {
                    // Sex
                    if (!labelSex.IsLabelFound
                        || labelSex.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> SEX");
                        SEX = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(DOB))
                {
                    // Date Of Birth
                    if (!labelDateOfBirth.IsLabelFound
                        || labelDateOfBirth.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> DOB");
                        DOB = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(ADDRESS_1))
                {
                    // the 1st line of Address
                    if (!labelAddress.IsLabelFound
                        || labelAddress.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_1");
                        ADDRESS_1 = line.Text;
                        idxMainColumn++;
                        lineAddress1 = line;
                        continue;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(ADDRESS_2))
                    {
                        // the 2nd line of Address
                        if (IsFieldJustUnderTheLine(lineAddress1, line))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> ADDRESS_2");
                            ADDRESS_2 = line.Text;
                                    continue;
                        }
                    }
                }

                if (string.IsNullOrEmpty(LICENSE_NO_EXPIRY))
                {
                    if (!labelLicense_No.IsLabelFound
                        || labelLicense_No.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO_EXPIRY");
                        LICENSE_NO_EXPIRY = line.Text;
                        idxMainColumn++;
                        // LICENSE_NO_EXPIRY -> documentNumber, documentExpirationDate
                        string[] splited = LICENSE_NO_EXPIRY.Split(new char[]{ ','}, StringSplitOptions.RemoveEmptyEntries); // ' ', 2);
                        if (splited != null && splited.Length > 0)
                        {
                            if (splited.Length > 1)
                            {
                                LICENSE_NO = splited[0].Trim();
                                EXPIRY = splited[1].Trim().Replace(" ", "").Trim();
                            }
                            else
                            {
                                LICENSE_NO = splited[0].Trim(); ;
                            }
                        }
                        continue;
                    }
                }
                /*
                if (string.IsNullOrEmpty(LICENSE_NO))
                {
                    if (!labelLicense_No.HasConfidence
                        || labelLicense_No.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> LICENSE_NO");
                        LICENSE_NO = line.Text;
                        confidence_LICENSE_NO = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }
                */
                if (string.IsNullOrEmpty(EXPIRY))
                {
                    if (!labelExpiration_Date.IsLabelFound
                        || labelExpiration_Date.IsFieldJustUnderTheLabel(line)
                        )
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> EXPIRY");
                        EXPIRY = line.Text;
                        idxMainColumn++;
                        continue;
                    }
                }

                // Unknown
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                idxMainColumn++;
            }// foreach lines in main column

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(LAST_NAME_FISRT_MIDDLE_NAME))
            {
                lsMissingFields.Add("LAST_NAME_FISRT_MIDDLE_NAME");
            }
            else
            {
                result.lastNameOrFullName = LAST_NAME_FISRT_MIDDLE_NAME;
            }


            string[] namesSplit = LAST_NAME_FISRT_MIDDLE_NAME.Split(SEPARATOR_NAME, 2);
            if (namesSplit != null && namesSplit.Length > 0)
            {
                result.lastNameOrFullName = namesSplit[0];

                if (namesSplit.Length > 1)
                {
                    if (namesSplit.Length > 2)
                    {
                        result.firstName = namesSplit[1];
                        result.middleName = namesSplit[2];
                    }
                    else
                    {
                        result.firstName = namesSplit[1];
                    }
                }
            }

            // LICENSE_NO -> documentNumber
            if (string.IsNullOrEmpty(LICENSE_NO))
            {
                lsMissingFields.Add("LICENSE_NO");
            }
            else
            {
                result.documentNumber = LICENSE_NO;
            }

            // EXPIRY "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            if (EXPIRY.Length == 10)
            {
                try
                {
                    int yyyy = int.Parse(EXPIRY.Substring(0, 4));
                    int MM = int.Parse(EXPIRY.Substring(5, 2));
                    int dd = int.Parse(EXPIRY.Substring(8, 2));
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    lsMissingFields.Add("EXPIRY");
                }
            }
            else
            {
                lsMissingFields.Add("EXPIRY");
            }

            // nationality 3 letter code
            if (string.IsNullOrEmpty(NATIONALITY))
            {
                lsMissingFields.Add("NATIONALITY");
            }
            else
            {
                result.nationality = NATIONALITY;
            }

            if (string.IsNullOrEmpty(SEX))
            {
                lsMissingFields.Add("SEX");
            }
            else
            {
                result.gender = SEX;
            }

            // DOB "yyyy/MM/dd" -> documentExpirationDate "yyyy-MM-dd"
            try
            {
                result.dateOfBirth = "";
                if (DOB.Length == 10)
                {
                    int yyyy = int.Parse(DOB.Substring(0, 4));
                    int MM = int.Parse(DOB.Substring(5, 2));
                    int dd = int.Parse(DOB.Substring(8, 2));
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                }
                else
                {
                    lsMissingFields.Add("DOB");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                lsMissingFields.Add("DOB");
            }

            // ADDRESS_1, ADDRESS_2 -> addressLine1, addressLine2
            if (string.IsNullOrEmpty(ADDRESS_1))
            {
                lsMissingFields.Add("ADDRESS_1");
            }
            else
            {
                result.addressLine1 = ADDRESS_1;
            }

            result.addressLine2 = ADDRESS_2;

            // determine success or not
            if (lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPHDL result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }
        static bool IsFieldJustUnderTheLine(Line line, Line field)
        {
            if (line != null)
            {
                if (Math.Abs((double)(field.ExtGetLeft() - line.ExtGetLeft())) < line.ExtGetHeight() * 3
                    && (double)(field.ExtGetTop() - line.ExtGetTop()) >= 0
                    && (double)(field.ExtGetTop() - line.ExtGetBottom()) < line.ExtGetHeight())
                {
                    return true;
                }
            }
            return false;
        }
        static bool IsFieldsUnderTheLine(Line line, Line field)
        {
            if ((double)(field.ExtGetTop() - line.ExtGetBottom()) >= 0)
            {
                return true;
            }
            return false;
        }

        static bool IsFieldInSameLeftEdgeOfLine(Line line, Line field)
        {
            //if (Math.Abs((double)(field.BoundingBox[0].Value - line.ExtGetLeft())) < line.ExtGetLeft() * 3)
            if (Math.Abs((double)(field.ExtGetLeft() - line.ExtGetLeft())) < line.ExtGetLeft() * 3)
            {
                return true;
            }
            return false;
        }

#if false
        public static ScanIDeKTPResult? ExtractFieldsFromReadResultOfIDeKTP(IList<Line> linesAll)
        {
            LabelInfo labelNIK = new("NIK");
            LabelInfo labelNama = new("Nama");
            LabelInfo labelTempatTglLahir = new("Tempat/Tgl Lahir");
            LabelInfo labelJenisKelamin = new("Jenis Kelamin");
            LabelInfo labelAlamat = new("Alamat");
            LabelInfo labelRT_RW = new("RT/RW");
            LabelInfo labelKel_Desa = new("Kel/Desa");
            LabelInfo labelKecamatan = new("Kecamatan");
            LabelInfo labelAgama = new("Agama");
            LabelInfo labelStatus_Perkawinan = new("Status Perkawinan");
            LabelInfo labelPekerjaan = new("Pekerjaan");
            LabelInfo labelKewarganegaraan = new("Kewarganegaraan");
            //LabelInfo labelBerlakuHingga = new("Berlaku Hingga"); // Berlaku Hingga (expiry date) is deprecated

            ScanIDeKTPResult? result = new ScanIDeKTPResult();

            string PROVINSI = "";   // 1st line
            Confidence confidence_PROVINSI = new Confidence();
            string KAB_KOTA = "";   // 2nd line (Kabupaten (Regency) or Kota (City))
            Confidence confidence_KAB_KOTA = new Confidence();
            string NIK = "";
            Confidence confidence_NIK = new Confidence();
            string NAMA = "";
            Confidence confidence_NAMA = new Confidence();
            string TEMPAT_TGL_LAHIR = ""; //PLACE_OF_BIRTH_DOB
            Confidence confidence_TEMPAT_TGL_LAHIR = new Confidence();
            //string PLACE_OF_BIRTH = ""; //PLACE_OF_BIRTH_DOB
            //Confidence confidence_PLACE_OF_BIRTH = new Confidence();
            //string DOB = ""; //PLACE_OF_BIRTH_DOB
            //Confidence confidence_DOB = new Confidence();
            string JENIS_KELAMIN = "";   // GENDER
            Confidence confidence_JENIS_KELAMIN = new Confidence();
            Line? valueJENIS_KELAMIN = null;
            string ALAMAT = "";
            Confidence confidence_ALAMAT = new Confidence();
            string RT_RW = "";
            Confidence confidence_RT_RW = new Confidence();
            string KEL_DESA = "";
            Confidence confidence_KEL_DESA = new Confidence();
            string KECAMATAN = "";
            Confidence confidence_KECAMATAN = new Confidence();
            string AGAMA = "";
            Confidence confidence_AGAMA = new Confidence();
            string STATUS_PERKAWINAN = "";
            Confidence confidence_STATUS_PERKAWINAN = new Confidence();
            string PEKERJAAN = "";
            Confidence confidence_PEKERJAAN = new Confidence();
            string KEWARGANEGARAAN = "";    // Nationality
            Confidence confidence_KEWARGANEGARAAN = new Confidence();
            //Line? valueKEWARGANEGARAAN = null;
            //string BERLAK_HINGGA = "";  // Expiry
            //Confidence confidence_BERLAK_HINGGA = new Confidence();

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // find labels exactly match
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelNIK.HasConfidence)
                {
                    if (labelNIK.MatchTitleExactly(line))
                        continue;
                }
                if (!labelNama.HasConfidence)
                {
                    if (labelNama.MatchTitleExactly(line))
                        continue;
                }
                if (!labelTempatTglLahir.HasConfidence)
                {
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ":", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ";", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    //if (labelTempatTglLahir.MatchTitleRegex(line, @"Tempat\/Tgl[ ]*Lahir"))
                    if (labelTempatTglLahir.MatchTitleRegex(line, @"Tempa.\/Tgl[ ]*Lahir"))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleFollowedByField(line, out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleExactly(line))
                        continue;
                }
                if (!labelJenisKelamin.HasConfidence)
                {
                    if (labelJenisKelamin.MatchTitleRegex(line, @"Jenis[ ]*Kelamin"))
                    {
                        confidence_JENIS_KELAMIN = confidence;
                        continue;
                    }
                    if (labelJenisKelamin.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAlamat.HasConfidence)
                {
                    if (labelAlamat.MatchTitleExactly(line))
                        continue;
                }
                if (!labelRT_RW.HasConfidence)
                {
                    if (labelRT_RW.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKel_Desa.HasConfidence)
                {
                    if (labelKel_Desa.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKecamatan.HasConfidence)
                {
                    if (labelKecamatan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelAgama.HasConfidence)
                {
                    if (labelAgama.MatchTitleExactly(line))
                        continue;
                }
                if (!labelStatus_Perkawinan.HasConfidence)
                {
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ":", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ";", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleFollowedByField(line, out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelPekerjaan.HasConfidence)
                {
                    if (labelPekerjaan.MatchTitleExactly(line))
                        continue;
                }
                if (!labelKewarganegaraan.HasConfidence)
                {
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ":", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ";", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleFollowedByField(line, out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleExactly(line))
                        continue;
                }
                //if (!labelBerlakuHingga.HasConfidence)
                //{
                //    if (labelBerlakuHingga.MatchTitleExactly(line))
                //        continue;
                //}

                linesFieldOrLabel.Add(line);
            }// foreach lines in other columns

            // find labels not found yet, and fields
            foreach (Line line in linesFieldOrLabel)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesFieldOrLabel {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelNIK.HasConfidence)
                {
                    if (labelNIK.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelNama.HasConfidence)
                {
                    if (labelNama.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelTempatTglLahir.HasConfidence)
                {
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ":", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleWithSeparator(line, ";", out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitleFollowedByField(line, out TEMPAT_TGL_LAHIR/*, mSpellSuggestion*/))
                    {
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        continue;
                    }
                    if (labelTempatTglLahir.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelJenisKelamin.HasConfidence)
                {
                    if (labelJenisKelamin.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelAlamat.HasConfidence)
                {
                    if (labelAlamat.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelRT_RW.HasConfidence)
                {
                    if (labelRT_RW.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKel_Desa.HasConfidence)
                {
                    if (labelKel_Desa.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKecamatan.HasConfidence)
                {
                    if (labelKecamatan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelAgama.HasConfidence)
                {
                    if (labelAgama.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelStatus_Perkawinan.HasConfidence)
                {
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ":", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleWithSeparator(line, ";", out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitleFollowedByField(line, out STATUS_PERKAWINAN/*, mSpellSuggestion*/))
                    {
                        confidence_STATUS_PERKAWINAN = confidence;
                        continue;
                    }
                    if (labelStatus_Perkawinan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelPekerjaan.HasConfidence)
                {
                    if (labelPekerjaan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                if (!labelKewarganegaraan.HasConfidence)
                {
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ":", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleWithSeparator(line, ";", out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitleFollowedByField(line, out KEWARGANEGARAAN/*, mSpellSuggestion*/))
                    {
                        confidence_KEWARGANEGARAAN = confidence;
                        continue;
                    }
                    if (labelKewarganegaraan.MatchTitle(line/*, mSpellSuggestion*/))
                        continue;
                }
                //if (!labelBerlakuHingga.HasConfidence)
                //{
                //    if (labelBerlakuHingga.MatchTitle(line/*, mSpellSuggestion*/))
                //        continue;
                //}

                linesField.Add(line);
            }// foreach lines in other columns

            // sort from top to bottom
            var linesLeftOrder = linesField.OrderBy(l => l.BoundingBox[0]);

            //int countLinesField = linesField.Count;
            //int idxMedianLinesField = countLinesField / 2;
            //double? leftMedian = linesLeftOrder.ElementAt(idxMedianLinesField).BoundingBox[0];

            //double? leftEdgeOfBlock = linesField.Min(l => l.BoundingBox[0]);
            //double? rightEdgeOfBlock = linesField.Max(l => l.BoundingBox[2]);
            //double? topEdgeOfBlock = linesField.Min(l => l.BoundingBox[1]);
            //double? bottomEdgeOfBlock = linesField.Max(l => l.BoundingBox[5]);
            //double? sumLeft = linesLeftOrder.Take(5).Sum(l => l.BoundingBox[0]);
            //double? avgLeft = sumLeft / 5;
            //double? acceptableRangeOfLeftEdge = (rightEdgeOfBlock - leftEdgeOfBlock) / 20;
            //double? h_center = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 2;
            //double? v_center = topEdgeOfBlock + (bottomEdgeOfBlock - topEdgeOfBlock) / 2;
            //double? h_leftSideEdge = leftEdgeOfBlock + (rightEdgeOfBlock - leftEdgeOfBlock) / 3;

            double? heightName = null;
            double? bottomName = null;
            int numLinesField = linesField.Count;
            int idxMainColumn = 0;

            foreach (Line line in linesField)
            {
                string text = line.Text.Trim();

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");

                if (heightName.HasValue)
                {
                    if (line.ExtGetHeight() < heightName * 0.65)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   Height:{line.ExtGetHeight()} < heightName:{heightName} * 0.65 = {heightName * 0.65} --> ignored");
                        //numLinesInMainColumn--;
                        numLinesField--;
                        continue;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] idxMainColumn:{idxMainColumn} numLinesField:{numLinesField}");

                // 
                // https://www.lingonomad.com/blogs/indonesia/administrative-divisions#:~:text=The%205%20Administrative%20Divisions%20of%20Indonesia%201%201.,5.%20Sub-district%2C%20known%20as%20%E2%80%9CKelurahan%E2%80%9D%20in%20Indonesian%20
                // https://en.wikipedia.org/wiki/List_of_regencies_and_cities_of_Indonesia
                if (string.IsNullOrEmpty(PROVINSI))
                {
                    // 1st line is PROVINSI
                    if (line.Text.Length > 4 && line.Text.Substring(0, 4) == "PROV")
                    {
                        PROVINSI = line.Text.Trim();
                        confidence_PROVINSI = confidence;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {PROVINSI} --> PROVINSI");
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(KAB_KOTA))
                {
                    // 2nd line is KAB_KOTA
                    if (line.Text.Length > 3 && line.Text.Substring(0, 3) == "KAB" || line.Text.Substring(0, 3) == "KOT")
                    {
                        KAB_KOTA = line.Text.Trim();
                        confidence_KAB_KOTA = confidence;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KAB_KOTA} --> KAB_KOTA");
                    }
                    continue;
                }

                if (string.IsNullOrEmpty(NIK))
                {
                    // NIK (IDNUM)
                    if (!labelNIK.HasConfidence
                        || labelNIK.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        NIK = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {NIK} --> NIK");
                        confidence_NIK = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(NAMA))
                {
                    // Name
                    if (!labelNama.HasConfidence
                        || labelNama.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        NAMA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {NAMA} --> NAMA");
                        confidence_NAMA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(TEMPAT_TGL_LAHIR))
                {
                    // Place Of Birth, DOB
                    if (!labelTempatTglLahir.HasConfidence
                        || labelTempatTglLahir.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        TEMPAT_TGL_LAHIR = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {TEMPAT_TGL_LAHIR} --> TEMPAT_TGL_LAHIR");
                        confidence_TEMPAT_TGL_LAHIR = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(JENIS_KELAMIN))
                {
                    // Gender
                    if (!labelJenisKelamin.HasConfidence
                        || labelJenisKelamin.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        JENIS_KELAMIN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {JENIS_KELAMIN} --> JENIS_KELAMIN");
                        confidence_JENIS_KELAMIN = confidence;
                        idxMainColumn++;
                        valueJENIS_KELAMIN = line;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(ALAMAT))
                {
                    // Alamat  (Addr line 1)
                    if (!labelAlamat.HasConfidence
                        || labelAlamat.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        ALAMAT = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {ALAMAT} --> ALAMAT");
                        confidence_ALAMAT = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(RT_RW))
                {
                    // RT_RW (Addr line 2)
                    if (!labelRT_RW.HasConfidence
                        || labelRT_RW.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        RT_RW = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {RT_RW} --> RT_RW");
                        confidence_RT_RW = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KEL_DESA))
                {
                    // KEL_DESA (Addr line 3)
                    if (!labelKel_Desa.HasConfidence
                        || labelKel_Desa.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KEL_DESA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KEL_DESA} --> KEL_DESA");
                        confidence_KEL_DESA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KECAMATAN))
                {
                    // KECAMATAN (Addr line 4)
                    if (!labelKecamatan.HasConfidence
                        || labelKecamatan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KECAMATAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KECAMATAN} --> KECAMATAN");
                        confidence_KECAMATAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(AGAMA))
                {
                    // AGAMA (Religion)
                    if (!labelAgama.HasConfidence
                        || labelAgama.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        AGAMA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {AGAMA} --> AGAMA");
                        confidence_AGAMA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(STATUS_PERKAWINAN))
                {
                    // STATUS_PERKAWINAN (Marital Status)
                    if (!labelStatus_Perkawinan.HasConfidence
                        || labelStatus_Perkawinan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        STATUS_PERKAWINAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {STATUS_PERKAWINAN} --> STATUS_PERKAWINAN");
                        confidence_STATUS_PERKAWINAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(PEKERJAAN))
                {
                    // PEKERJAAN (Job Status)
                    if (!labelPekerjaan.HasConfidence
                        || labelPekerjaan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        PEKERJAAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {PEKERJAAN} --> PEKERJAAN");
                        confidence_PEKERJAAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(KEWARGANEGARAAN))
                {
                    // KEWARGANEGARAAN (Nationality)
                    if (!labelKewarganegaraan.HasConfidence
                        || labelKewarganegaraan.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        KEWARGANEGARAAN = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {KEWARGANEGARAAN} --> KEWARGANEGARAAN");
                        confidence_KEWARGANEGARAAN = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }
                /*
                if (string.IsNullOrEmpty(BERLAK_HINGGA))
                {
                    // BERLAK_HINGGA (Expiry)
                    if (!labelBerlakuHingga.HasConfidence
                        || labelBerlakuHingga.IsFieldRightNextToTheLabel(line)
                        )
                    {
                        BERLAK_HINGGA = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {BERLAK_HINGGA} --> BERLAK_HINGGA");
                        confidence_BERLAK_HINGGA = confidence;
                        idxMainColumn++;
                        continue;
                    }
                }
                */
                // Unknown
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {line.Text} --> UNKNOWN");
                idxMainColumn++;
            }// foreach lines in main column

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // PROVINSI
            if (string.IsNullOrEmpty(PROVINSI))
            {
                lsMissingFields.Add("PROVINSI");
            }
            else
            {
                result.provinsi = PROVINSI;
                result.provinsiConfidence = confidence_PROVINSI;
            }

            // KAB/LPTA
            if (string.IsNullOrEmpty(KAB_KOTA))
            {
                lsMissingFields.Add("KAB_KOTA");
            }
            else
            {
                result.kabKota = KAB_KOTA;
                result.kabKotaConfidence = confidence_KAB_KOTA;
            }

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(NAMA))
            {
                lsMissingFields.Add("NAMA");
            }
            else
            {
                result.lastNameOrFullName = NAMA;
                result.lastNameOrFullNameConfidence = confidence_NAMA;
            }

            // NIK -> documentNumber
            if (string.IsNullOrEmpty(NIK))
            {
                lsMissingFields.Add("NIK");
            }
            else
            {
                result.documentNumber = NIK;
                result.documentNumberConfidence = confidence_NIK;
            }

            // TEMPAT_TGL_LAHIR -> Place of birth, Date Of Birth
            if (TEMPAT_TGL_LAHIR.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    //Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    Regex regexDDMMYYYY = new Regex(@"\d{2}[ -]\d{2}[ -]\d{4}");
                    MatchCollection matches = regexDDMMYYYY.Matches(TEMPAT_TGL_LAHIR);
                    if (matches.Count >= 0)
                    {
                        string yyyyMMdd = matches[0].Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.dateOfBirthConfidence = confidence_TEMPAT_TGL_LAHIR;

                            int posDoB = TEMPAT_TGL_LAHIR.IndexOf(yyyyMMdd);
                            if (posDoB > 0)
                            {
                                // extract place of birth
                                result.placeOfBirth = TEMPAT_TGL_LAHIR.Substring(0, posDoB).Trim().Trim(',');
                                result.placeOfBirthConfidence = confidence_TEMPAT_TGL_LAHIR;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.dateOfBirth))
            {
                lsMissingFields.Add("dateOfBirth");
            }
            if (string.IsNullOrEmpty(result.placeOfBirth))
            {
                lsMissingFields.Add("placeOfBirth");
            }

            // Gender
            if (valueJENIS_KELAMIN != null)
            {
                if (CheckCharInLine(valueJENIS_KELAMIN, new Regex("LAKI[-| ]*LAKI")))
                {
                    result.gender = "M";
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                }
                else if (CheckCharInLine(valueJENIS_KELAMIN, "PEREMPUAN"))
                {
                    result.gender = "F";
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                }
                else
                {
                    // unknown...
                    result.gender = valueJENIS_KELAMIN.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();
                    result.genderConfidence = confidence_JENIS_KELAMIN;
                    lsMissingFields.Add("gender");
                }
            }
            else
            {
                lsMissingFields.Add("gender");
            }

            // Address
            if (string.IsNullOrEmpty(ALAMAT))
            {
                lsMissingFields.Add("ALAMAT");
            }
            else
            {
                result.addressLine1 = ALAMAT;
                result.addressLine1Confidence = confidence_ALAMAT;
                result.alamat = ALAMAT;
                result.alamatConfidence = confidence_ALAMAT;
            }

            //result.addressLine2 = $"{RT_RW} {KEL_DESA} {KECAMATAN}";
            //result.addressLine2Confidence = confidence_RT_RW + confidence_KEL_DESA + confidence_KECAMATAN;
            if (string.IsNullOrEmpty(RT_RW))
            {
                lsMissingFields.Add("RT_RW");
            }
            else
            {
                string[] arrRtRw = RT_RW.Split('/');
                if (arrRtRw.Length > 0)
                {
                    result.rt = arrRtRw[0].Trim();
                    result.rtConfidence = confidence_RT_RW;
                }
                if (arrRtRw.Length > 1)
                {
                    result.rw = arrRtRw[1].Trim();
                    result.rwConfidence = confidence_RT_RW;
                }
            }
            if (string.IsNullOrEmpty(KEL_DESA))
            {
                lsMissingFields.Add("KEL_DESA");
            }
            result.addressLine2 = $"{RT_RW} {KEL_DESA}";
            result.addressLine2Confidence = confidence_RT_RW + confidence_KEL_DESA;
            result.kelDesa = KEL_DESA;
            result.kelDesaConfidence = confidence_KEL_DESA;

            if (string.IsNullOrEmpty(KECAMATAN))
            {
                lsMissingFields.Add("KECAMATAN");
            }
            else
            {
                result.addressTown = $"{KECAMATAN}";
                result.addressTownConfidence = confidence_KECAMATAN;
                result.kecamatan = KECAMATAN;
                result.kecamatanConfidence = confidence_KECAMATAN;
            }

            // Marital Status
            /*
            Civil Status:		
            S	Single	
            M	Married	
            X	Separated	
            W	Widow/er

            1. Belum Kawin = SINGLE --> S (Single)	
            2. Kawin = MARRIED --> M (Married)
            3. Cerai Hidup = DIVORCED --> X	(Separated)
            4. Cerai Mati = WIDOWED --> W (Widow/er)
            */
            if (!string.IsNullOrWhiteSpace(STATUS_PERKAWINAN))
            {
                if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "BELUM KAWIN")) // Single
                {
                    result.maritalStatus = "S";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "KAWIN"))   // Married
                {
                    result.maritalStatus = "M";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "CERAI HIDUP"))   // Separated
                {
                    result.maritalStatus = "X";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else if (CheckCharInLine(STATUS_PERKAWINAN, confidence_STATUS_PERKAWINAN, "CERAI MATI"))   // Widow/er
                {
                    result.maritalStatus = "W";
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                }
                else
                {
                    // unknown...
                    result.maritalStatus = STATUS_PERKAWINAN;
                    result.maritalStatusConfidence = confidence_STATUS_PERKAWINAN;
                    lsMissingFields.Add("STATUS_PERKAWINAN");
                }
            }
            else
            {
                lsMissingFields.Add("STATUS_PERKAWINAN");
            }

            // nationality 3 letter code
            result.nationality = KEWARGANEGARAAN;
            result.nationalityConfidence = confidence_KEWARGANEGARAAN;
            if (!string.IsNullOrEmpty(KEWARGANEGARAAN))
            {
                if (CheckCharInLine(KEWARGANEGARAAN, confidence_KEWARGANEGARAAN, "WNI"))
                {
                    result.nationality = "IDN";
                }
            }
            if (string.IsNullOrEmpty(result.nationality))
            {
                lsMissingFields.Add("STATUS_PERKAWINAN");
            }

            /*
            // BERLAK_HINGGA "dd-MM-yyyy" -> documentExpirationDate "yyyy-MM-dd"
            if (BERLAK_HINGGA.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    MatchCollection matches = regexDDMMYYYY.Matches(BERLAK_HINGGA);
                    if (matches.Count >= 0)
                    {
                        string yyyyMMdd = matches[0].Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.documentExpirationDateConfidence = confidence_BERLAK_HINGGA;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.documentExpirationDate))
            {
                lsMissingFields.Add("BERLAK_HINGGA");
            }
            */

            // determine success or not
            if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfIDeKTP result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }
#endif
#if false
        public static ScanIDDLResult? ExtractFieldsFromReadResultOfIDDL(IList<Line> linesAll)
        {
            /* https://en.wikipedia.org/wiki/Driving_license_in_Indonesia
            1. [NAME]                                                                                    
            2. PLACE, DATE OF BIRTH:[DOB in dd-mm-yyyy]
            3. (BLOOD TYPE (A/B/AB/O) - [Sex: Pria=Male, Wanita=Female]
            4. (ADDRESS)
            5. OCCUPATION: [occupation in Indonesia]
            6. PROVINCE OF REGISTRATION 
            */
            LabelInfo labelINDONESIA = new("INDONESIA");
            LabelInfo labelSURAT_IZIN_MENGEMUDI = new("SURAT IZIN MENGEMUDI");
            LabelInfo labelDRIVING_LICENSE = new("DRIVING LICENSE");

            ScanIDDLResult? result = new ScanIDDLResult();

            Regex regexIDNum = new Regex(@"\d{4}-\d{4}-\d{6}");
            int idxOfItem = -1;

            string IDNUM = "";
            Confidence confidence_IDNUM = new Confidence();
            string L1_NAME = "";
            Confidence confidence_L1_NAME = new Confidence();
            string L2_PLACE_DATE_OF_BIRTH = ""; // XXXX, DD-MM-YYYY
            Confidence confidence_L2_PLACE_DATE_OF_BIRTH = new Confidence();
            string L3_BLOODTYPE_SEX = "";    // A/B/AB/O - PRIA/WANITA
            Confidence confidence_L3_BLOODTYPE_SEX = new Confidence();
            string L4_ADDRESS1 = "";
            Confidence confidence_L4_ADDRESS1 = new Confidence();
            string L4_ADDRESS2 = "";
            Confidence confidence_L4_ADDRESS2 = new Confidence();
            string L4_ADDRESS3 = "";
            Confidence confidence_L4_ADDRESS3 = new Confidence();
            string L5_OCCUPATION = "";
            Confidence confidence_L5_OCCUPATION = new Confidence();
            string L6_PROVINCE_OF_REGISTRATION = "";
            Confidence confidence_L6_PROVINCE_OF_REGISTRATION = new Confidence();

            List<Line> linesField = new List<Line>();   // lines valid and not label
            List<Line> linesFieldOrLabel = new List<Line>();   // lines valid and not label

            // find labels exactly match
            foreach (Line line in linesAll)
            {
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] linesAll {line.Text} Height:{line.ExtGetHeight()} Min:{confidence.Min} Avg:{confidence.Avg} Max:{confidence.Max}");
                if (confidence.Avg < 0.5)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   confidence.avg:{confidence.Avg} < 0.5 --> ignored");
                    continue;
                }

                double? angle = line.ExtGetAngle();
                if (angle == null || Math.Abs((decimal)angle) > 10)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]   angle:{angle} > 10 --> ignored");
                    continue;
                }

                if (!labelINDONESIA.HasConfidence)
                {
                    if (labelINDONESIA.MatchTitleExactly(line))
                        continue;
                }
                if (!labelSURAT_IZIN_MENGEMUDI.HasConfidence)
                {
                    if (labelSURAT_IZIN_MENGEMUDI.MatchTitleExactly(line))
                        continue;
                }
                if (!labelDRIVING_LICENSE.HasConfidence)
                {
                    if (labelDRIVING_LICENSE.MatchTitleExactly(line))
                        continue;
                }

                try
                {
                    Match matchIDNUM = regexIDNum.Match(text);
                    if (matchIDNUM.Success)
                    {
                        IDNUM = matchIDNUM.Value;
                        idxOfItem = 0;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                if (idxOfItem > -1)
                {
                    string strLineNumAndBlank = "";
                    string strLineFollowingLineNum = "";
                    //string strRexEx = @"^\d{1}[.,]?\D";
                    string strRexEx = $"^{idxOfItem + 1}[.,]?\\s?";
                    if (MatchRegExInLine(line, strRexEx, out strLineNumAndBlank))
                    {
                        strLineFollowingLineNum = line.Text.Substring(strLineNumAndBlank.Length).Trim();
                        idxOfItem++;
                        switch (idxOfItem)
                        {
                            //1. [NAME]                                                                                    
                            case 1:
                                L1_NAME = strLineFollowingLineNum;
                                confidence_L1_NAME = confidence;
                                break;
                            //2. PLACE, DATE OF BIRTH:[DOB in dd-mm-yyyy]
                            case 2:
                                L2_PLACE_DATE_OF_BIRTH = strLineFollowingLineNum;
                                confidence_L2_PLACE_DATE_OF_BIRTH = confidence;
                                break;
                            //3. (BLOOD TYPE (A/B/AB/O) - [Sex: Pria=Male, Wanita=Female]
                            case 3:
                                L3_BLOODTYPE_SEX = strLineFollowingLineNum;
                                confidence_L3_BLOODTYPE_SEX = confidence;
                                break;
                            //4. (ADDRESS)
                            case 4:
                                L4_ADDRESS1 = strLineFollowingLineNum;
                                confidence_L4_ADDRESS1 = confidence;
                                break;
                            //5. OCCUPATION: [occupation in Indonesia]
                            case 5:
                                L5_OCCUPATION = strLineFollowingLineNum;
                                confidence_L5_OCCUPATION = confidence;
                                break;
                            //6. PROVINCE OF REGISTRATION 
                            case 6:
                                L6_PROVINCE_OF_REGISTRATION = strLineFollowingLineNum;
                                confidence_L6_PROVINCE_OF_REGISTRATION = confidence;
                                break;
                            default:
                                System.Diagnostics.Debug.WriteLine($"Unexpected idxOfItem:{idxOfItem} line.Text:{line.Text}");
                                break;
                        }
                    }
                    else
                    {
                        if (idxOfItem == 4)
                        {
                            if (string.IsNullOrEmpty(L4_ADDRESS2))
                            {
                                L4_ADDRESS2 = line.Text.Trim();
                                confidence_L4_ADDRESS2 = confidence;
                            }
                            else if (string.IsNullOrEmpty(L4_ADDRESS3))
                            {
                                L4_ADDRESS3 = line.Text.Trim();
                                confidence_L4_ADDRESS3 = confidence;
                            }
                        }
                    }
                }

                linesFieldOrLabel.Add(line);
            }// foreach lines in other columns

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            // NAME -> lastNameOrFullName 
            if (string.IsNullOrEmpty(L1_NAME))
            {
                lsMissingFields.Add("NAME");
            }
            result.lastNameOrFullName = L1_NAME;
            result.lastNameOrFullNameConfidence = confidence_L1_NAME;

            // IDNUM -> documentNumber
            if (string.IsNullOrEmpty(IDNUM))
            {
                lsMissingFields.Add("IDNUM");
            }
            result.documentNumber = IDNUM;
            result.documentNumberConfidence = confidence_IDNUM;

            // Place of birth, Date Of Birth
            if (L2_PLACE_DATE_OF_BIRTH.Length >= 10)
            {
                try
                {
                    // dd-MM-yyyy
                    Regex regexDDMMYYYY = new Regex(@"\d{2}-\d{2}-\d{4}");
                    Match matche = regexDDMMYYYY.Match(L2_PLACE_DATE_OF_BIRTH);
                    if (matche.Success)
                    {
                        string yyyyMMdd = matche.Value;
                        if (yyyyMMdd.Length == 10)
                        {
                            int dd = int.Parse(yyyyMMdd.Substring(0, 2));
                            int MM = int.Parse(yyyyMMdd.Substring(3, 2));
                            int yyyy = int.Parse(yyyyMMdd.Substring(6, 4));
                            result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                            result.dateOfBirthConfidence = confidence_L2_PLACE_DATE_OF_BIRTH;

                            int posDoB = L2_PLACE_DATE_OF_BIRTH.IndexOf(yyyyMMdd);
                            if (posDoB > 0)
                            {
                                // extract place of birth
                                result.placeOfBirth = L2_PLACE_DATE_OF_BIRTH.Substring(0, posDoB).Trim().Trim(',').Trim('.');
                                result.placeOfBirthConfidence = confidence_L2_PLACE_DATE_OF_BIRTH;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
            if (string.IsNullOrEmpty(result.dateOfBirth))
            {
                lsMissingFields.Add("dateOfBirth");
            }
            if (string.IsNullOrEmpty(result.placeOfBirth))
            {
                lsMissingFields.Add("placeOfBirth");
            }

            // Gender
            if (L3_BLOODTYPE_SEX != null)
            {
                // dd-MM-yyyy
                Regex regexBloodType = new Regex("^(A|B|AB|O)");
                Match matche = regexBloodType.Match(L3_BLOODTYPE_SEX);
                if (matche.Success)
                {
                    string bloodType = matche.Value;
                    string strSex = L3_BLOODTYPE_SEX.Substring(bloodType.Length).Trim().Trim(',').Trim('.').Trim('-').Trim();
                    if (CheckCharInLine(strSex, confidence_L3_BLOODTYPE_SEX, "PRIA"/*, mSpellSuggestion*/))
                    {
                        result.gender = "M";
                        result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                    }
                    else if (CheckCharInLine(L3_BLOODTYPE_SEX, confidence_L3_BLOODTYPE_SEX, "WANITA"/*, mSpellSuggestion*/))
                    {
                        result.gender = "F";
                        result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                    }
                    else
                    {
                        // unknown...
                        int posSex = L3_BLOODTYPE_SEX.IndexOf(bloodType);
                        if (posSex > 0)
                        {
                            result.gender = L3_BLOODTYPE_SEX.Substring(0, posSex).Trim().Trim(',').Trim('.').Trim('-').Trim();
                            result.genderConfidence = confidence_L3_BLOODTYPE_SEX;
                            lsMissingFields.Add("gender");
                        }
                    }
                }
            }
            if (string.IsNullOrEmpty(result.gender))
            {
                lsMissingFields.Add("gender");
            }

            // Address
            result.addressLine1 = L4_ADDRESS1;
            result.addressLine1Confidence = confidence_L4_ADDRESS1;
            if (string.IsNullOrEmpty(L4_ADDRESS1))
            {
                lsMissingFields.Add("ADDRESS");
            }

            result.addressLine2 = $"{L4_ADDRESS2} {L4_ADDRESS3}";
            result.addressLine2Confidence = confidence_L4_ADDRESS2;
            if (confidence_L4_ADDRESS3.Avg > 0)
                result.addressLine2Confidence += confidence_L4_ADDRESS3;

            // determine success or not
            if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
            {
                result.Success = true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfIDDL result NOT success");
                if (lsMissingFields.Count > 0)
                {
                    string fields = "";
                    foreach (string field in lsMissingFields)
                    {
                        if (!string.IsNullOrEmpty(fields))
                            fields += ",";
                        fields += field;
                    }
                    result.Error = $"Failed to scan [{fields}]";
                }
            }

            return result;
        }
#endif
#if false
        public static ScanPassportMRZResult? ExtractFieldsFromReadResultOfPassportMRZ(IList<Line> linesAll)
        {
            ScanPassportMRZResult? result = new ScanPassportMRZResult();

            string ISSUING_COUNTRY = "";
            Confidence confidence_ISSUING_COUNTRY = new Confidence();
            string FULL_NAME = "";
            Confidence confidence_FULL_NAME = new Confidence();
            string SURNAME = "";
            Confidence confidence_SURNAME = new Confidence();
            string GIVEN_NAME = "";
            Confidence confidence_GIVEN_NAME = new Confidence();
            string PASSPORT_NUMBER = "";
            Confidence confidence_PASSPORT_NUMBER = new Confidence();
            string NATIONALITY = "";
            Confidence confidence_NATIONALITY = new Confidence();
            string DOB = "";
            Confidence confidence_DOB = new Confidence();
            string SEX = "";
            Confidence confidence_SEX = new Confidence();
            string EXPIRY = "";
            Confidence confidence_EXPIRY = new Confidence();
            string PERSONAL_NUMBER = "";
            Confidence confidence_PERSONAL_NUMBER = new Confidence();

            string MRZ1 = "";
            Confidence confidence_MRZ1 = new Confidence();
            string MRZ2 = "";
            Confidence confidence_MRZ2 = new Confidence();

            // find MRZ which start with 'P>'
            //https://en.wikipedia.org/wiki/Machine-readable_passport

            foreach (Line line in linesAll)
            {
                string strLine = line.Text.Trim().Replace(" ", ""); // remove all blank space
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                if (string.IsNullOrEmpty(MRZ1))
                {
                    /*
                    P<JPNTATEISHI << TAKUMI <<<<<<<<<<<<<<<<<<<<<<<
                    TZ11450519JPN7104181M2608042 <<<<<<<<<<<<<< 02
                    (blank space in MRZ line should be removed)
                    P<JPNTATEISHI<<TAKUMI<<<<<<<<<<<<<<<<<<<<<<<
                    TZ11450519JPN7104181M2608042<<<<<<<<<<<<<<02
                    */
                    string strLastOrFullName = "";
                    string strFirstName = "";
                    string strLastName = "";
                    if (strLine.StartsWith("P") && strLine.Length == 44)
                    {
                        MRZ1 = strLine;
                        confidence_MRZ1 = confidence;
                        System.Diagnostics.Debug.WriteLine($"MRZ1: {MRZ1}");
                        /*
                        1       1   alpha P, indicating a passport
                        2       1   alpha +< Type(for countries that distinguish between different types of passports)
                        3–5     3   alpha Issuing country or organization(ISO 3166 - 1 alpha - 3 code with modifications)
                        6–44    39  alpha +< Surname, followed by two filler characters, 
                                    followed by given names. Given names are separated by single filler characters. 
                                    Some countries do not differentiate between surname and given name(i.e.no two filler characters), such as the Malaysian Passport
                        */
                        // 3-5 (3) Issuing country or organization(ISO 3166 - 1 alpha - 3 code with modifications)
                        ISSUING_COUNTRY = strLine.Substring(2, 3);
                        confidence_ISSUING_COUNTRY = confidence;

                        // 6–44 (39) Surname, followed by two filler characters, 
                        //              followed by given names. Given names are separated by single filler characters. 
                        //              Some countries do not differentiate between surname and given name(i.e.no two filler characters), such as the Malaysian Passport
                        string blockName = strLine.Substring(5);
                        string[] names = blockName.Split("<<");
                        List<string> lsName = new List<string>();
                        foreach (string name in names)
                        {
                            if (!string.IsNullOrEmpty(name) && name != "<")
                            {
                                lsName.Add(name);
                            }
                        }
                        if (lsName.Count > 0)
                        {
                            if (lsName.Count == 1)
                            {
                                FULL_NAME = lsName[0].Replace("<", " ");
                                confidence_FULL_NAME = confidence;
                                System.Diagnostics.Debug.WriteLine($"FULL_NAME: {FULL_NAME}");
                            }
                            else if (lsName.Count == 2)
                            {
                                SURNAME = lsName[0].Replace("<", " ");
                                confidence_SURNAME = confidence;
                                GIVEN_NAME = lsName[1].Replace("<", " ");
                                confidence_GIVEN_NAME = confidence;
                                System.Diagnostics.Debug.WriteLine($"SURNAME: {SURNAME}");
                                System.Diagnostics.Debug.WriteLine($"GIVEN_NAME: {GIVEN_NAME}");
                            }
                            else
                            {
                                FULL_NAME = blockName.Replace("<", " ").Trim();
                                System.Diagnostics.Debug.WriteLine($"FULL_NAME: {FULL_NAME}");
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("----------");
                    }
                }
                else
                {
                    MRZ2 = strLine;
                    confidence_MRZ2 = confidence;
                    System.Diagnostics.Debug.WriteLine($"MRZ2: {MRZ2}");
                    /*
                    1–9	9	alpha+num+<	Passport number
                    10	1	numeric	Check digit over digits 1–9
                    11–13	3	alpha+<	Nationality or Citizenship (ISO 3166-1 alpha-3 code with modifications)
                    14–19	6	numeric	Date of birth (YYMMDD)
                    20	1	numeric	Check digit over digits 14–19
                    21	1	alpha+<	Sex (M, F or < for male, female or unspecified)
                    22–27	6	numeric	Expiration date of passport (YYMMDD)
                    28	1	numeric	Check digit over digits 22–27
                    29–42	14	alpha+num+<	Personal number (may be used by the issuing country as it desires)
                    43	1	numeric+<	Check digit over digits 29–42 (may be < if all characters are <)
                    44	1	numeric	Check digit over digits 1–10, 14–20, and 22–43
                    */
                    // 1-9  (9) Passport number
                    PASSPORT_NUMBER = strLine.Substring(0, 9);
                    confidence_PASSPORT_NUMBER = confidence;
                    System.Diagnostics.Debug.WriteLine($"PASSPORT_NUMBER: {PASSPORT_NUMBER}");

                    // 10   (1)	Check digit over digits 1–9
                    string CheckDigit_1_9 = strLine.Substring(9, 1);
                    string digits_1_9 = strLine.Substring(0, 9);
                    int chk = CalcCheckSum(digits_1_9);
                    if (CheckDigit_1_9 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_1_9:{CheckDigit_1_9} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_1_9:{CheckDigit_1_9} does not match calculated number:{chk} !!!");
                    }

                    // 11–13 (3) Nationality or Citizenship (ISO 3166-1 alpha-3 code with modifications)
                    NATIONALITY = strLine.Substring(10, 3);
                    confidence_NATIONALITY = confidence;
                    System.Diagnostics.Debug.WriteLine($"NATIONALITY: {NATIONALITY}");

                    // 14–19 (6) Date of birth (YYMMDD)
                    DOB = strLine.Substring(13, 6);
                    confidence_DOB = confidence;
                    System.Diagnostics.Debug.WriteLine($"DateOfBirth: {DOB}");

                    // 20    (1) Check digit over digits 14–19
                    string CheckDigit_14_19 = strLine.Substring(19, 1);
                    string digits_14_19 = strLine.Substring(13, 6);
                    chk = CalcCheckSum(digits_14_19);
                    if (CheckDigit_14_19 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_14_19:{CheckDigit_14_19} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_14_19:{CheckDigit_14_19} does not match calculated number:{chk} !!!");
                    }

                    // 21	 (1) Sex (M, F or < for male, female or unspecified)
                    SEX = strLine.Substring(20, 1);
                    confidence_SEX = confidence;
                    System.Diagnostics.Debug.WriteLine($"SEX: {SEX}");

                    // 22–27 (6) Expiration date of passport (YYMMDD)
                    EXPIRY = strLine.Substring(21, 6);
                    confidence_EXPIRY = confidence;
                    System.Diagnostics.Debug.WriteLine($"EXPIRY: {EXPIRY}");

                    // 28    (1) Check digit over digits 22–27
                    string CheckDigit_22_27 = strLine.Substring(27, 1);
                    string digits_22_27 = strLine.Substring(21, 6);
                    chk = CalcCheckSum(digits_22_27);
                    if (CheckDigit_22_27 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! digits_22_27:{CheckDigit_22_27} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! digits_22_27:{CheckDigit_22_27} does not match calculated number:{chk} !!!");
                    }

                    // 29–42 (14) Personal number (may be used by the issuing country as it desires)
                    PERSONAL_NUMBER = strLine.Substring(28, 14).Replace("<", " ").Trim();
                    confidence_PERSONAL_NUMBER = confidence;
                    System.Diagnostics.Debug.WriteLine($"PERSONAL_NUMBER: {PERSONAL_NUMBER}");

                    // 43    (1) Check digit over digits 29–42 (may be < if all characters are <)
                    string CheckDigit_29_42 = strLine.Substring(42, 1);
                    string digits_29_42 = strLine.Substring(28, 14);
                    if (CheckDigit_29_42 != "<")
                    {
                        chk = CalcCheckSum(digits_29_42);
                        if (CheckDigit_29_42 != chk.ToString())
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_29_42:{CheckDigit_29_42} does not match calculated number:{chk} !!!");
                            throw new Exception($"!!! CheckDigit_29_42:{CheckDigit_29_42} does not match calculated number:{chk} !!!");
                        }
                    }
                    else
                    {
                        if (digits_29_42 != "<<<<<<<<<<<<<<")
                        {
                            System.Diagnostics.Debug.WriteLine($"!!! digits_29_42:{digits_29_42} does not match the check digit:{CheckDigit_29_42} which expect all chars are '<' !!!");
                            throw new Exception($"!!! digits_29_42:{digits_29_42} does not match the check digit:{CheckDigit_29_42} which expect all chars are '<' !!!");
                        }
                    }

                    // 44    (1) Check digit over digits 1–10, 14–20, and 22–43
                    string CheckDigit_1_10__14_20__22_43 = strLine.Substring(43, 1);
                    string digits_1_10__14_20__22_43 = strLine.Substring(0, 10) + strLine.Substring(13, 7) + strLine.Substring(21, 22);
                    chk = CalcCheckSum(digits_1_10__14_20__22_43);
                    if (CheckDigit_1_10__14_20__22_43 != chk.ToString())
                    {
                        System.Diagnostics.Debug.WriteLine($"!!! CheckDigit_1_10__14_20__22_43:{CheckDigit_1_10__14_20__22_43} does not match calculated number:{chk} !!!");
                        throw new Exception($"!!! CheckDigit_1_10__14_20__22_43:{CheckDigit_1_10__14_20__22_43} does not match calculated number:{chk} !!!");
                    }

                    System.Diagnostics.Debug.WriteLine("----------");
                    result.Success = true;
                    break;
                }
            }// foreach lines in other columns

            // map to result and convert format 
            List<string> lsMissingFields = new List<string>();

            if (result.Success)
            {
                // ISSUING_COUNTRY
                Country? country = FindCountryBy3LetterCode(ISSUING_COUNTRY);
                if (country != null)
                {
                    result.SetCountry(country);
                }
                else
                {
                    lsMissingFields.Add("ISSUING_COUNTRY");
                }

                if (!string.IsNullOrEmpty(FULL_NAME))
                {
                    // FULL_NAME -> lastNameOrFullName 
                    result.lastNameOrFullName = FULL_NAME;
                    result.lastNameOrFullNameConfidence = confidence_FULL_NAME;
                }
                else
                {
                    // SURNAME -> lastNameOrFullName 
                    result.lastNameOrFullName = SURNAME;
                    result.lastNameOrFullNameConfidence = confidence_SURNAME;
                    // GIVEN_NAME -> firstName
                    result.firstName = GIVEN_NAME;
                    result.firstNameConfidence = confidence_GIVEN_NAME;
                }
                if (string.IsNullOrEmpty(result.lastNameOrFullName))
                {
                    lsMissingFields.Add("NAME");
                }

                // PASSPORT_NUMBER
                result.documentNumber = PASSPORT_NUMBER;
                result.documentNumberConfidence = confidence_PASSPORT_NUMBER;
                if (string.IsNullOrEmpty(result.documentNumber))
                {
                    lsMissingFields.Add("PASSPORT_NUMBER");
                }

                // NATIONALITY
                if (!string.IsNullOrEmpty(NATIONALITY))
                {
                    Country? nationality = Code.FindCountryBy3LetterCode(NATIONALITY);
                    if (nationality != null)
                    {
                        result.nationality = nationality.ncode;
                    }
                }
                if (string.IsNullOrEmpty(NATIONALITY))
                {
                    lsMissingFields.Add("PASSPORT_NUMBER");
                }

                // DOB
                if (!string.IsNullOrEmpty(DOB))
                {
                    /*
                    https://www.ibm.com/docs/en/i/7.3?topic=mcdtdi-conversion-2-digit-years-4-digit-years-centuries
                    If a 2-digit year is moved to a 4-digit year, the century (1st 2 digits of the year) are chosen as follows:
                      - If the 2-digit year is greater than or equal to 40, the century used is 1900. In other words, 19 becomes the first 2 digits of the 4-digit year.
                      - If the 2-digit year is less than 40, the century used is 2000. In other words, 20 becomes the first 2 digits of the 4-digit year.
                    */
                    string dobYY = DOB.Substring(0, 2);
                    string dobMM = DOB.Substring(2, 2);
                    string dobDD = DOB.Substring(4, 2);
                    int dd = int.Parse(dobDD);
                    int MM = int.Parse(dobMM);
                    int yy = int.Parse(dobYY);
                    int yyyy = 1900 + yy;
                    if (yy < 40)
                    {
                        yyyy = 2000 + yy;
                    }
                    result.dateOfBirth = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    result.dateOfBirthConfidence = confidence_DOB;
                }
                if (string.IsNullOrEmpty(result.dateOfBirth))
                {
                    lsMissingFields.Add("dateOfBirth");
                }

                // SEX
                result.gender = SEX;
                result.genderConfidence = confidence_SEX;
                if (string.IsNullOrEmpty(result.gender))
                {
                    lsMissingFields.Add("SEX");
                }

                // EXPIRY
                if (!string.IsNullOrEmpty(EXPIRY))
                {
                    string expYY = EXPIRY.Substring(0, 2);
                    string expMM = EXPIRY.Substring(2, 2);
                    string expDD = EXPIRY.Substring(4, 2);
                    int dd = int.Parse(expDD);
                    int MM = int.Parse(expMM);
                    int yy = int.Parse(expYY);
                    int yyyy = 1900 + yy;
                    if (yy < 40)
                    {
                        yyyy = 2000 + yy;
                    }
                    result.documentExpirationDate = $"{yyyy:0000}-{MM:00}-{dd:00}";
                    result.documentExpirationDateConfidence = confidence_EXPIRY;
                }
                else
                {
                    lsMissingFields.Add("EXPIRY");
                }

                // PERSONAL_NUMBER
                if (!string.IsNullOrEmpty(PERSONAL_NUMBER))
                {
                    result.personalNumber = PERSONAL_NUMBER;
                    result.personalNumberConfidence = confidence_PERSONAL_NUMBER;
                }
                else
                {
                    // PERSONAL_NUMBER is optional
                }

                // determine success or not
                if (result.confidences.Worst > 0 && result.confidences.Avg > 0.7 && lsMissingFields.Count == 0)
                {
                    result.Success = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ExtractFieldsFromReadResultOfPassportMRZ result NOT success");
                    if (lsMissingFields.Count > 0)
                    {
                        string fields = "";
                        foreach (string field in lsMissingFields)
                        {
                            if (!string.IsNullOrEmpty(fields))
                                fields += ",";
                            fields += field;
                        }
                        result.Error = $"Failed to scan [{fields}]";
                    }
                }
            }

            return result;
        }
#endif

        static string CorrectFalseParsedNumericLine(string text)
        {
            string ret = "";
            foreach (char c in text)
            {
                switch (c)
                {
                    case 'o':
                    case 'O':
                        ret += '0';
                        break;
                    case 'l':
                        ret += '1';
                        break;
                    case 'b':
                        ret += '6';
                        break;
                    case 'd':
                        ret += '8';
                        break;
                    default:
                        ret += c;
                        break;
                }
            }
            return ret;
        }

        public static bool CheckCharInLine(Line line, string textExpected)
        {
            return CheckCharInLine(line.Text, textExpected);
            /*
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine line:{line} value:{textExpected}");
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    strRegex += $"{c}?";
                    if (!textExpected.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    strRegex += ".";
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    if (countCharNotIn < 3 && countCharIn > textExpected.Length - 3 && regexLine.Match(textExpected).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {textExpected}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
            */
        }

        public static bool CheckCharInLine(string text, string textExpected)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine text:{text} value:{textExpected}");
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                text = text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    strRegex += $"{c}?";
                    if (!textExpected.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    strRegex += ".";
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    if (countCharNotIn < 3 && countCharIn > textExpected.Length - 3 && regexLine.Match(textExpected).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {textExpected}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{text}] exception:{ex}");
                return false;
            }
        }

        public static bool CheckCharInLine(Line line, Regex regexLine)
        {
            return CheckCharInLine(line.Text, regexLine);
            /*
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine line:{line} regexLine:{regexLine}");
            try
            {
                bool bRet = false;
                string text = line.Text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                try
                {
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    Match match = regexLine.Match(text);
                    if (match.Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {match.Value}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{line}] exception:{ex}");
                return false;
            }
            */
        }
        public static bool CheckCharInLine(string text, Regex regexLine)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine text:{text} regexLine:{regexLine}");
            try
            {
                bool bRet = false;
                text = text.Replace(":", String.Empty).Replace(";", String.Empty).Trim();

                try
                {
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {textExpected}");
                    Match match = regexLine.Match(text);
                    if (match.Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {match.Value}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                return bRet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine [{text}] exception:{ex}");
                return false;
            }
        }
#if true
        static public List<Line> OCRLinesWithTesseractB64(string b64Image, string language = "eng" )
        {
            using (var image = Pix.LoadFromMemory(Convert.FromBase64String(b64Image)))
            {
                return OCRLinesWithTesseractPix(image);
            }
        }

        static public List<Line> OCRLinesWithTesseractEncodedData(byte[] data)
        {
            using (var image = Pix.LoadFromMemory(data))
            {
                return OCRLinesWithTesseractPix(image);
            }
        }

        static TesseractEngine _tesseractEngine = null;

        static TesseractEngine GetTesseractEngine()
        {
            if(_tesseractEngine == null)
            {
                _tesseractEngine = new TesseractEngine("./tessdata", "eng", EngineMode.Default);
            }
            return _tesseractEngine;
        }
        static List<Line> OCRLinesWithTesseractPix(Pix image)
        {
            List<Line> lines = new List<Line>();
            using (var page = GetTesseractEngine().Process(image))
            {
                //ret = page.GetText();
                ResultIterator ri = page.GetIterator();
                float fConf = page.GetMeanConfidence();
                ri.Begin();

                do
                {
                    string text = ri.GetText(PageIteratorLevel.TextLine);
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim(new char[] { '\n', ' ' });
                        Console.WriteLine(text);
                        Rect rcBoundingBox;
                        Line line = new Line();
                        line.Text = text;
                        line.Confidence = fConf;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Text: {line.Text} ({fConf})");
                        if (ri.TryGetBoundingBox(PageIteratorLevel.TextLine, out rcBoundingBox))
                        {
                            //line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y1,
                            //        (double)rcBoundingBox.X2, (double)rcBoundingBox.Y2, (double)rcBoundingBox.X1, (double)rcBoundingBox.Y2 };
                            line.BoundingBox = new List<double?> { (double)rcBoundingBox.X1, (double)rcBoundingBox.Y1, (double)rcBoundingBox.X2, (double)rcBoundingBox.Y2 };
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] rcBoundingBox: {rcBoundingBox.X1} {rcBoundingBox.Y1} {rcBoundingBox.X2} {rcBoundingBox.Y2}");
                        }

                        Rect rcBaseline;
                        if (ri.TryGetBaseline(PageIteratorLevel.TextLine, out rcBaseline))
                        {
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] rcBaseLine: {rcBaseline.X1} {rcBaseline.Y1} {rcBaseline.X2} {rcBaseline.Y2}");
                            line.Baseline = new List<double?> { (double)rcBaseline.X1, (double)rcBaseline.Y1, (double)rcBaseline.X2, (double)rcBaseline.Y2 };
                        }
                        lines.Add(line);
                    }
                } while (ri.Next(PageIteratorLevel.TextLine));
            }

            return lines;
        }
#endif
    }
}
