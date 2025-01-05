// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json.Linq;
//using ScanIDLib;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using ScanID;
//using ConsoleApp1;

const string BASEADDR_URL2 = "http://127.0.0.1:8085/";
const string BASEADDR_URL1 = "http://127.0.0.1:8086/";
//const string BASEADDR_URL1 = "";

// read test json 
if (args.Length != 1)
{
    Console.WriteLine("Usage: test1.exe <test json file>");
    return;
}

string testfile = args[0];
FileInfo fi = new FileInfo(testfile);
if (!fi.Exists)
{
    Console.WriteLine($"File [{testfile}] not found.");
    return;
}

JArray jArray = JArray.Parse(File.ReadAllText(testfile));

JsonSerializerOptions jsonOptions = new JsonSerializerOptions() { AllowTrailingCommas = true };

// for each of json array, OCR test image and comapre the result with expected data.
foreach (JObject jObject in jArray)
{
    string imageFileName = jObject["image"].ToString();
    string imageBackFileName = "";
    if (jObject.ContainsKey("imageBack"))
    {
        imageBackFileName = jObject["imageBack"].ToString();
    }
    string dataFileName = jObject["data"].ToString();
    FileInfo? fiImage = null;
    FileInfo? fiImageBack = null;
    FileInfo? fiData = null;
    if (File.Exists(imageFileName))
    {
        fiImage = new FileInfo(imageFileName);
    }
    else
    {
        if(fi.DirectoryName == null)
        {
            Console.WriteLine($"Test json file [{fi.Name}], directory not found.");
            continue;
        }
        // search in the same folder as test json
        fiImage = new FileInfo(Path.Combine(fi.DirectoryName, imageFileName));
        if (!fiImage.Exists)
        {
            Console.WriteLine($"Image file [{imageFileName}] not found.");
            continue;
        }
    }

    if (!string.IsNullOrEmpty(imageBackFileName))
    {
        if (File.Exists(imageBackFileName))
        {
            fiImageBack = new FileInfo(imageBackFileName);
        }
        else
        {
            // search in the same folder as test json
            fiImageBack = new FileInfo(Path.Combine(fi.DirectoryName, imageBackFileName));
            if (!fiImageBack.Exists)
            {
                Console.WriteLine($"Image file [{imageBackFileName}] not found.");
                continue;
            }
        }
    }

    if (File.Exists(dataFileName))
    {
        fiData = new FileInfo(dataFileName);
    }
    else
    {
        // search in the same folder as test json
        fiData = new FileInfo(Path.Combine(fi.DirectoryName, dataFileName));
        if (!fiData.Exists)
        {
            Console.WriteLine($"Data file [{dataFileName}] not found.");
            continue;
        }
    }

    TestImageFile(fiImage, fiImageBack, fiData);

} // foreach

#if false
void TestImageFile(FileInfo fiImage, FileInfo fiImageBack, FileInfo fiData)
{
    /////////////
    StreamContent fileImage = new StreamContent(fiImage.OpenRead());
    MultipartFormDataContent form = new MultipartFormDataContent();
    switch (fiImage.Extension.ToLower())
    {
        case ".pdf":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            break;
        case ".jpg":
        case ".jpeg":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            break;
        case ".png":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            break;
        default:
            throw new Exception($"File type not supported.[{fiImage.Extension}]");
            break;
    }
    form.Add(fileImage, "idImage", fiImage.Name);

    if (fiImageBack != null)
    {
        StreamContent fileImageBack = new StreamContent(fiImageBack.OpenRead());
        switch (fiImageBack.Extension.ToLower())
        {
            case ".pdf":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                break;
            case ".jpg":
            case ".jpeg":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                break;
            case ".png":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                break;
            default:
                throw new Exception($"File type not supported.[{fiImageBack.Extension}]");
                break;
        }
        form.Add(fileImageBack, "idImageBack", fiImageBack.Name);
    }

    Console.Write($"{fiImage.Name} : ");

    // post
    string resultJson = "";
    using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) })
    {
        DateTime dtStart = DateTime.Now;
        //var response = httpClient.PostAsync($"https://localhost/csekycwebapiazfacedebug/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        var response = httpClient.PostAsync($"https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        DateTime dtEnd = DateTime.Now;
        var elapsed = dtEnd - dtStart;
        Console.Write(" Elapsed: " + elapsed);
        System.Diagnostics.Debug.WriteLine("StatusCode: " + response.StatusCode);
        System.Diagnostics.Debug.WriteLine("Headers: " + response.Headers);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            System.Diagnostics.Debug.WriteLine("Content: " + response.Content);
            throw new Exception(response.ReasonPhrase);
        }

        if (response.Content != null)
        {
            resultJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("Content: " + resultJson);

            JObject jObjectResult = JObject.Parse(resultJson);
            if (jObjectResult["documentType"] != null)
            {
                string country = jObjectResult["country"].ToString();
                string documentType = jObjectResult["documentType"].ToString();
                Console.Write($" {country} : {documentType} : ");
                using (StreamReader sr = new StreamReader(fiData.OpenRead()))
                {
                    string expectedDataJson = sr.ReadToEnd();
                    switch (country)
                    {
                        /*
MYDL_TZ1145051_Test1.jpg :  Elapsed: 00:00:27.4395177 MY : DL :  --> IsValid: True
MYDL_S10701105740_Test1.jpg :  Elapsed: 00:00:19.2677005 MY : DL :  --> IsValid: True
MYKAD_730818-13-5230_Test1.jpg :  Elapsed: 00:00:10.0879668 MY : MY :   Error : Unexpected documentType [MY]
PhilID_2031-8439-2460-5291_Test1.jpg :  Elapsed: 00:00:08.6591035 PH : NI :   Error : Unexpected country [PH]
CRN-0111-8184420-7_Test1.jpg :  Elapsed: 00:00:15.7811114 PH : UI :   Error : Unexpected country [PH]
CRN-0111-2588996-9_Test1.jpg :  Elapsed: 00:00:15.2136485 PH : UI :   Error : Unexpected country [PH]
CRN-0033-5508686-0_Test1.jpg :  Elapsed: 00:00:08.6318526 PH : UI :   Error : Unexpected country [PH]
                         */
                        case "MY":
                            if (documentType == "DL")
                            {
                                ScanMYDLResult? dataExpected = JsonSerializer.Deserialize<ScanMYDLResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanMYDLResult? result = JsonSerializer.Deserialize<ScanMYDLResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + result);
                                    bool isValid = IsScanMYDLResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine($"  Error : Data expected [{fiData.Name}] is null.");
                                }
                            }
                            else if (documentType == "MY")
                            {
                                ScanMyKadResult? dataExpected = JsonSerializer.Deserialize<ScanMyKadResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanMyKadResult? result = JsonSerializer.Deserialize<ScanMyKadResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanMyKadResult: " + result);
                                    bool isValid = IsScanMyKadResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine($"  Error : Data expected [{fiData.Name}] is null.");
                                }
                            }
                            else
                            {
                                ScanIDResult? result = JsonSerializer.Deserialize<ScanIDResult>(resultJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("ScanIDResult: " + result);
                                Console.WriteLine($"  Error : Unexpected documentType [{documentType}]");
                            }
                            break;
                        case "PH":
                            if (documentType == "NI")
                            {
                                ScanPHNIResult? dataExpected = JsonSerializer.Deserialize<ScanPHNIResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanPHNIResult? result = JsonSerializer.Deserialize<ScanPHNIResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanPHNIResult: " + result);
                                    bool isValid = IsScanPHNIResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine($"  Error : Data expected [{fiData.Name}] is null.");
                                }
                            }
                            else if (documentType == "UI")
                            {
                                ScanPHUMIDResult? dataExpected = JsonSerializer.Deserialize<ScanPHUMIDResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanPHUMIDResult? result = JsonSerializer.Deserialize<ScanPHUMIDResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanMyKadResult: " + result);
                                    bool isValid = IsScanPHUMIDResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine($"  Error : Data expected [{fiData.Name}] is null.");
                                }
                            }
                            else
                            {
                                ScanIDResult? result = JsonSerializer.Deserialize<ScanIDResult>(resultJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("ScanIDResult: " + result);
                                Console.WriteLine($"  Error : Unexpected documentType [{documentType}]");
                            }
                            break;
                        default:
                            Console.WriteLine($"  Error : Unexpected country [{country}]");
                            ScanIDResult? dataExpected_ = JsonSerializer.Deserialize<ScanIDResult>(expectedDataJson, jsonOptions);
                            break;
                    }
                }
            }
        }
    }
    /////////////
}
#else
void TestImageFile(FileInfo fiImage, FileInfo fiImageBack, FileInfo fiData)
{
    string expectedDataJson = "";
    string expectedCountry = "";
    string expectedDocumentType = "";
    JObject? jObjectExpectedResult = null;

    using (StreamReader sr = new StreamReader(fiData.OpenRead()))
    {
        expectedDataJson = sr.ReadToEnd();
        try
        {
            jObjectExpectedResult = JObject.Parse(expectedDataJson);
            if (jObjectExpectedResult != null)
            {
                expectedCountry = jObjectExpectedResult["country"].ToString();
                expectedDocumentType = jObjectExpectedResult["documentType"].ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Json Parse exception: {ex}");
        }
    }

    bool isValid = false;
    switch (expectedCountry)
    {
        case "MY":
            switch (expectedDocumentType)
            {
                case "DL":
                    try
                    {
                        if(!string.IsNullOrEmpty(BASEADDR_URL1))
                        {
                            ScanMYDLResult scanMYDLResult = ScanID.ScanIDOCR.ScanMYDL(BASEADDR_URL1, fiImage.FullName);
                            Console.WriteLine($"scanMYDLResult.Success: {scanMYDLResult.Success}");
                            Console.WriteLine($"scanMYDLResult.Error: {scanMYDLResult.Error}");
                            Console.WriteLine($"scanMYDLResult.lastNameOrFullName: {scanMYDLResult.lastNameOrFullName}");
                            Console.WriteLine($"scanMYDLResult.documentNumber: {scanMYDLResult.documentNumber}");
                            Console.WriteLine($"scanMYDLResult.nationality: {scanMYDLResult.nationality}");
                            Console.WriteLine($"scanMYDLResult.documentIssueDate: {scanMYDLResult.documentIssueDate}");
                            Console.WriteLine($"scanMYDLResult.documentExpirationDate: {scanMYDLResult.documentExpirationDate}");
                            Console.WriteLine($"scanMYDLResult.addressLine1: {scanMYDLResult.addressLine1}");
                            Console.WriteLine($"scanMYDLResult.addressLine2: {scanMYDLResult.addressLine2}");
                            Console.WriteLine($"scanMYDLResult.postcode: {scanMYDLResult.postcode}");
                            ScanMYDLResult? dataExpected = JsonSerializer.Deserialize<ScanMYDLResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + scanMYDLResult);
                            isValid = IsScanMYDLResultValid(scanMYDLResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }

                        if (!isValid && !string.IsNullOrEmpty(BASEADDR_URL2))
                        {
                            Console.WriteLine("Try another OCR...");
                            ScanMYDLResult scanMYDLResult = ScanID.ScanIDOCR.ScanMYDL(BASEADDR_URL2, fiImage.FullName);
                            Console.WriteLine($"scanMYDLResult.Success: {scanMYDLResult.Success}");
                            Console.WriteLine($"scanMYDLResult.Error: {scanMYDLResult.Error}");
                            Console.WriteLine($"scanMYDLResult.lastNameOrFullName: {scanMYDLResult.lastNameOrFullName}");
                            Console.WriteLine($"scanMYDLResult.documentNumber: {scanMYDLResult.documentNumber}");
                            Console.WriteLine($"scanMYDLResult.nationality: {scanMYDLResult.nationality}");
                            Console.WriteLine($"scanMYDLResult.documentIssueDate: {scanMYDLResult.documentIssueDate}");
                            Console.WriteLine($"scanMYDLResult.documentExpirationDate: {scanMYDLResult.documentExpirationDate}");
                            Console.WriteLine($"scanMYDLResult.addressLine1: {scanMYDLResult.addressLine1}");
                            Console.WriteLine($"scanMYDLResult.addressLine2: {scanMYDLResult.addressLine2}");
                            Console.WriteLine($"scanMYDLResult.postcode: {scanMYDLResult.postcode}");
                            ScanMYDLResult? dataExpected = JsonSerializer.Deserialize<ScanMYDLResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + scanMYDLResult);
                            isValid = IsScanMYDLResultValid(scanMYDLResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
#if false
                        {
                            // Try Tesseract
                            if (!System.IO.File.Exists(fiImage.FullName))
                            {
                                Console.WriteLine("File not found: " + fiImage.FullName);
                            }
                            else
                            {
                                SkiaSharp.SKImage bmpImage = SkiaSharp.SKImage.FromEncodedData(fiImage.FullName);
                                int width = bmpImage.Width;
                                int height = bmpImage.Height;

                                string b64Image = "";
                                using (var stream = System.IO.File.OpenRead(fiImage.FullName))
                                {
                                    byte[] b = new byte[stream.Length];
                                    stream.Read(b, 0, b.Length);
                                    b64Image = Convert.ToBase64String(b);
                                }

                                if (b64Image.Length == 0)
                                {
                                    Console.WriteLine("File is empty: " + fiImage.FullName);
                                }
                                else
                                {
                                    Console.WriteLine($"[{fiImage.FullName}]");
                                    {
                                        DateTime dtStart = DateTime.Now;
                                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                                        //Console.WriteLine(retOcr);
                                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image);
                                        DateTime dtEnd = DateTime.Now;
                                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                                        // remove </s> from the start of 1st line
                                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                                        {
                                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                                        }

                                        Console.WriteLine("Lines extrected by Tesseract:");
                                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                                        foreach (Line line in linesTess)
                                        {
                                            if (linesTessDict.ContainsKey(line.Text))
                                            {
                                                Console.WriteLine($"  {line.Text} --> duplicated");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"  {line.Text}");
                                                linesTessDict[line.Text] = line;
                                            }
                                        }

                                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                                        ScanMYDLResult scanMYDLResult = ScanIDOCR.ExtractFieldsFromReadResultOfMYDL(linesTessRemoveDuplicated, width, bmpImage);
                                        Console.WriteLine($"scanMYDLResult.Success: {scanMYDLResult.Success}");
                                        Console.WriteLine($"scanMYDLResult.Error: {scanMYDLResult.Error}");
                                        Console.WriteLine($"scanMYDLResult.lastNameOrFullName: {scanMYDLResult.lastNameOrFullName}");
                                        Console.WriteLine($"scanMYDLResult.documentNumber: {scanMYDLResult.documentNumber}");
                                        Console.WriteLine($"scanMYDLResult.nationality: {scanMYDLResult.nationality}");
                                        Console.WriteLine($"scanMYDLResult.documentIssueDate: {scanMYDLResult.documentIssueDate}");
                                        Console.WriteLine($"scanMYDLResult.documentExpirationDate: {scanMYDLResult.documentExpirationDate}");
                                        Console.WriteLine($"scanMYDLResult.addressLine1: {scanMYDLResult.addressLine1}");
                                        Console.WriteLine($"scanMYDLResult.addressLine2: {scanMYDLResult.addressLine2}");
                                        Console.WriteLine($"scanMYDLResult.postcode: {scanMYDLResult.postcode}");
                                    }
                                }
                            }
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanMYDL exception: {ex}");
                    }
                    break;
                case "MY":
                    try
                    {
                        if (!string.IsNullOrEmpty(BASEADDR_URL1))
                        {
                            ScanMyKadResult scanMyKadResult = ScanIDOCR.ScanMyKad(BASEADDR_URL1, fiImage.FullName);
                            Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                            Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                            Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                            Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                            Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                            Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                            Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                            ScanMyKadResult? dataExpected = JsonSerializer.Deserialize<ScanMyKadResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + scanMyKadResult);
                            isValid = IsScanMyKadResultValid(scanMyKadResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                        if (!isValid && !string.IsNullOrEmpty(BASEADDR_URL2))
                        {
                            Console.WriteLine("Try another OCR...");
                            ScanMyKadResult scanMyKadResult = ScanIDOCR.ScanMyKad(BASEADDR_URL2, fiImage.FullName);
                            Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                            Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                            Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                            Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                            Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                            Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                            Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                            ScanMyKadResult? dataExpected = JsonSerializer.Deserialize<ScanMyKadResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + scanMyKadResult);
                            isValid = IsScanMyKadResultValid(scanMyKadResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
#if false
                        {
                            // try tesseract
                            if (!System.IO.File.Exists(fiImage.FullName))
                            {
                                Console.WriteLine("File not found: " + fiImage.FullName);
                            }
                            else
                            {
                                SkiaSharp.SKImage bmpImage = SkiaSharp.SKImage.FromEncodedData(fiImage.FullName);
                                int width = bmpImage.Width;
                                int height = bmpImage.Height;

                                string b64Image = "";
                                using (var stream = System.IO.File.OpenRead(fiImage.FullName))
                                {
                                    byte[] b = new byte[stream.Length];
                                    stream.Read(b, 0, b.Length);
                                    b64Image = Convert.ToBase64String(b);
                                }

                                if (b64Image.Length == 0)
                                {
                                    Console.WriteLine("File is empty: " + fiImage.FullName);
                                }
                                else
                                {
                                    Console.WriteLine($"[{fiImage.FullName}]");
                                    {
                                        DateTime dtStart = DateTime.Now;
                                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                                        //Console.WriteLine(retOcr);
                                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image);
                                        DateTime dtEnd = DateTime.Now;
                                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                                        // remove </s> from the start of 1st line
                                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                                        {
                                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                                        }

                                        Console.WriteLine("Lines extrected by Tesseract:");
                                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                                        foreach (Line line in linesTess)
                                        {
                                            if (linesTessDict.ContainsKey(line.Text))
                                            {
                                                Console.WriteLine($"  {line.Text} --> duplicated");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"  {line.Text}");
                                                linesTessDict[line.Text] = line;
                                            }
                                        }

                                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                                        ScanMyKadResult scanMyKadResult = ScanIDOCR.ExtractFieldsFromReadResultOfMyKad(linesTessRemoveDuplicated, bmpImage);
                                        Console.WriteLine($"scanMyKadResult.Success: {scanMyKadResult.Success}");
                                        Console.WriteLine($"scanMyKadResult.Error: {scanMyKadResult.Error}");
                                        Console.WriteLine($"scanMyKadResult.lastNameOrFullName: {scanMyKadResult.lastNameOrFullName}");
                                        Console.WriteLine($"scanMyKadResult.documentNumber: {scanMyKadResult.documentNumber}");
                                        Console.WriteLine($"scanMyKadResult.addressLine1: {scanMyKadResult.addressLine1}");
                                        Console.WriteLine($"scanMyKadResult.addressLine2: {scanMyKadResult.addressLine2}");
                                        Console.WriteLine($"scanMyKadResult.postcode: {scanMyKadResult.postcode}");
                                    }
                                }
                            }
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanMyKad exception: {ex}");
                    }
                    break;
                default:
                    Console.WriteLine($"Unexpected documentType: {expectedDocumentType}");
                    break;
            }
            break;
        case "PH":
            switch (expectedDocumentType)
            {
                case "UI":
                    try
                    {
                        if (!string.IsNullOrEmpty(BASEADDR_URL1))
                        {
                            ScanID.ScanPHUMIDResult scanPHUMIDResult = ScanID.ScanIDOCR.ScanPHUMID(BASEADDR_URL1, fiImage.FullName);
                            Console.WriteLine($"scanPHUMIDResult.Success: {scanPHUMIDResult.Success}");
                            Console.WriteLine($"scanPHUMIDResult.Error: {scanPHUMIDResult.Error}");
                            Console.WriteLine($"scanPHUMIDResult.lastNameOrFullName: {scanPHUMIDResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHUMIDResult.documentNumber: {scanPHUMIDResult.documentNumber}");
                            Console.WriteLine($"scanPHUMIDResult.nationality: {scanPHUMIDResult.nationality}");
                            Console.WriteLine($"scanPHUMIDResult.documentIssueDate: {scanPHUMIDResult.documentIssueDate}");
                            Console.WriteLine($"scanPHUMIDResult.documentExpirationDate: {scanPHUMIDResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHUMIDResult.addressLine1: {scanPHUMIDResult.addressLine1}");
                            Console.WriteLine($"scanPHUMIDResult.addressLine2: {scanPHUMIDResult.addressLine2}");
                            Console.WriteLine($"scanPHUMIDResult.postcode: {scanPHUMIDResult.postcode}");
                            ScanPHUMIDResult? dataExpected = JsonSerializer.Deserialize<ScanPHUMIDResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanPHUMIDResult: " + scanPHUMIDResult);
                            isValid = IsScanPHUMIDResultValid(scanPHUMIDResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                        if (!isValid && !string.IsNullOrEmpty(BASEADDR_URL2))
                        {
                            Console.WriteLine("Try another OCR...");
                            ScanID.ScanPHUMIDResult scanPHUMIDResult = ScanID.ScanIDOCR.ScanPHUMID(BASEADDR_URL2, fiImage.FullName);
                            Console.WriteLine($"scanPHUMIDResult.Success: {scanPHUMIDResult.Success}");
                            Console.WriteLine($"scanPHUMIDResult.Error: {scanPHUMIDResult.Error}");
                            Console.WriteLine($"scanPHUMIDResult.lastNameOrFullName: {scanPHUMIDResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHUMIDResult.documentNumber: {scanPHUMIDResult.documentNumber}");
                            Console.WriteLine($"scanPHUMIDResult.nationality: {scanPHUMIDResult.nationality}");
                            Console.WriteLine($"scanPHUMIDResult.documentIssueDate: {scanPHUMIDResult.documentIssueDate}");
                            Console.WriteLine($"scanPHUMIDResult.documentExpirationDate: {scanPHUMIDResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHUMIDResult.addressLine1: {scanPHUMIDResult.addressLine1}");
                            Console.WriteLine($"scanPHUMIDResult.addressLine2: {scanPHUMIDResult.addressLine2}");
                            Console.WriteLine($"scanPHUMIDResult.postcode: {scanPHUMIDResult.postcode}");
                            ScanPHUMIDResult? dataExpected = JsonSerializer.Deserialize<ScanPHUMIDResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanPHUMIDResult: " + scanPHUMIDResult);
                            isValid = IsScanPHUMIDResultValid(scanPHUMIDResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
#if false
                        {
                            // try tesseract
                            if (!System.IO.File.Exists(fiImage.FullName))
                            {
                                Console.WriteLine("File not found: " + fiImage.FullName);
                            }
                            else
                            {
                                SkiaSharp.SKImage bmpImage = SkiaSharp.SKImage.FromEncodedData(fiImage.FullName);
                                int width = bmpImage.Width;
                                int height = bmpImage.Height;

                                string b64Image = "";
                                using (var stream = System.IO.File.OpenRead(fiImage.FullName))
                                {
                                    byte[] b = new byte[stream.Length];
                                    stream.Read(b, 0, b.Length);
                                    b64Image = Convert.ToBase64String(b);
                                }

                                if (b64Image.Length == 0)
                                {
                                    Console.WriteLine("File is empty: " + fiImage.FullName);
                                }
                                else
                                {
                                    Console.WriteLine($"[{fiImage.FullName}]");
                                    {
                                        DateTime dtStart = DateTime.Now;
                                        //string retOcr = PostOCRWithRegionRequest(b64Image);
                                        //Console.WriteLine(retOcr);
                                        //List<Line> lines = ScanID.PostOCRWithRegionRequest(BASEADDR_URL, b64Image);
                                        List<Line> linesTess = ScanIDOCR.OCRLinesWithTesseractB64(b64Image, "eng+ocra");
                                        DateTime dtEnd = DateTime.Now;
                                        Console.WriteLine($"({(dtEnd - dtStart).TotalSeconds} sec)\n");

                                        // remove </s> from the start of 1st line
                                        if (linesTess.Count > 0 && linesTess[0].Text.StartsWith("</s>"))
                                        {
                                            linesTess[0].Text = linesTess[0].Text.Replace("</s>", "");
                                        }

                                        Console.WriteLine("Lines extrected by Tesseract:");
                                        List<Line> linesTessRemoveDuplicated = new List<Line>();
                                        Dictionary<string, Line> linesTessDict = new Dictionary<string, Line>();
                                        foreach (Line line in linesTess)
                                        {
                                            if (linesTessDict.ContainsKey(line.Text))
                                            {
                                                Console.WriteLine($"  {line.Text} --> duplicated");
                                            }
                                            else
                                            {
                                                Console.WriteLine($"  {line.Text}");
                                                linesTessDict[line.Text] = line;
                                            }
                                        }

                                        linesTessRemoveDuplicated = linesTessDict.Values.ToList();

                                        ScanPHUMIDResult scanPHUMIDResult = ScanIDOCR.ExtractFieldsFromReadResultOfPHUMID(linesTessRemoveDuplicated, bmpImage);
                                        Console.WriteLine($"scanPHUMIDResult.Success: {scanPHUMIDResult.Success}");
                                        Console.WriteLine($"scanPHUMIDResult.Error: {scanPHUMIDResult.Error}");
                                        Console.WriteLine($"scanPHUMIDResult.lastNameOrFullName: {scanPHUMIDResult.lastNameOrFullName}");
                                        Console.WriteLine($"scanPHUMIDResult.documentNumber: {scanPHUMIDResult.documentNumber}");
                                        Console.WriteLine($"scanPHUMIDResult.addressLine1: {scanPHUMIDResult.addressLine1}");
                                        Console.WriteLine($"scanPHUMIDResult.addressLine2: {scanPHUMIDResult.addressLine2}");
                                        Console.WriteLine($"scanPHUMIDResult.postcode: {scanPHUMIDResult.postcode}");
                                    }
                                }
                            }
                        }
#endif
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanPHUMID exception: {ex}");
                    }
                    break;
                case "DL":
                    try
                    {
                        if (!string.IsNullOrEmpty(BASEADDR_URL1))
                        {
                            ScanID.ScanPHDLResult scanPHDLResult = ScanID.ScanIDOCR.ScanPHDL(BASEADDR_URL1, fiImage.FullName);
                            Console.WriteLine($"scanPHDLResult.Success: {scanPHDLResult.Success}");
                            Console.WriteLine($"scanPHDLResult.Error: {scanPHDLResult.Error}");
                            Console.WriteLine($"scanPHDLResult.lastNameOrFullName: {scanPHDLResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHDLResult.documentNumber: {scanPHDLResult.documentNumber}");
                            Console.WriteLine($"scanPHDLResult.nationality: {scanPHDLResult.nationality}");
                            Console.WriteLine($"scanPHDLResult.documentIssueDate: {scanPHDLResult.documentIssueDate}");
                            Console.WriteLine($"scanPHDLResult.documentExpirationDate: {scanPHDLResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHDLResult.addressLine1: {scanPHDLResult.addressLine1}");
                            Console.WriteLine($"scanPHDLResult.addressLine2: {scanPHDLResult.addressLine2}");
                            Console.WriteLine($"scanPHDLResult.postcode: {scanPHDLResult.postcode}");
                            ScanPHDLResult? dataExpected = JsonSerializer.Deserialize<ScanPHDLResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanPHDLResult: " + scanPHDLResult);
                            isValid = IsScanPHDLResultValid(scanPHDLResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                        if (!isValid && !string.IsNullOrEmpty(BASEADDR_URL2))
                        {
                            Console.WriteLine("Try another OCR...");
                            ScanID.ScanPHDLResult scanPHDLResult = ScanID.ScanIDOCR.ScanPHDL(BASEADDR_URL2, fiImage.FullName);
                            Console.WriteLine($"scanPHDLResult.Success: {scanPHDLResult.Success}");
                            Console.WriteLine($"scanPHDLResult.Error: {scanPHDLResult.Error}");
                            Console.WriteLine($"scanPHDLResult.lastNameOrFullName: {scanPHDLResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHDLResult.documentNumber: {scanPHDLResult.documentNumber}");
                            Console.WriteLine($"scanPHDLResult.nationality: {scanPHDLResult.nationality}");
                            Console.WriteLine($"scanPHDLResult.documentIssueDate: {scanPHDLResult.documentIssueDate}");
                            Console.WriteLine($"scanPHDLResult.documentExpirationDate: {scanPHDLResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHDLResult.addressLine1: {scanPHDLResult.addressLine1}");
                            Console.WriteLine($"scanPHDLResult.addressLine2: {scanPHDLResult.addressLine2}");
                            Console.WriteLine($"scanPHDLResult.postcode: {scanPHDLResult.postcode}");
                            ScanPHDLResult? dataExpected = JsonSerializer.Deserialize<ScanPHDLResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("ScanPHDLResult: " + scanPHDLResult);
                            isValid = IsScanPHDLResultValid(scanPHDLResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanPHDL exception: {ex}");
                    }
                    break;
                case "NI":
                    try
                    {
                        if (!string.IsNullOrEmpty(BASEADDR_URL1))
                        {
                            ScanID.ScanPHNIResult scanPHNIResult = ScanID.ScanIDOCR.ScanPHNI(BASEADDR_URL1, fiImage.FullName, fiImageBack.FullName);
                            Console.WriteLine($"scanPHNIResult.Success: {scanPHNIResult.Success}");
                            Console.WriteLine($"scanPHNIResult.Error: {scanPHNIResult.Error}");
                            Console.WriteLine($"scanPHNIResult.lastNameOrFullName: {scanPHNIResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHNIResult.documentNumber: {scanPHNIResult.documentNumber}");
                            Console.WriteLine($"scanPHNIResult.nationality: {scanPHNIResult.nationality}");
                            Console.WriteLine($"scanPHNIResult.documentIssueDate: {scanPHNIResult.documentIssueDate}");
                            Console.WriteLine($"scanPHNIResult.documentExpirationDate: {scanPHNIResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHNIResult.addressLine1: {scanPHNIResult.addressLine1}");
                            Console.WriteLine($"scanPHNIResult.addressLine2: {scanPHNIResult.addressLine2}");
                            Console.WriteLine($"scanPHNIResult.postcode: {scanPHNIResult.postcode}");
                            ScanPHNIResult? dataExpected = JsonSerializer.Deserialize<ScanPHNIResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("scanPHNIResult: " + scanPHNIResult);
                            isValid = IsScanPHNIResultValid(scanPHNIResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                        if (!isValid && !string.IsNullOrEmpty(BASEADDR_URL2))
                        {
                            Console.WriteLine("Try another OCR...");
                            ScanID.ScanPHNIResult scanPHNIResult = ScanID.ScanIDOCR.ScanPHNI(BASEADDR_URL2, fiImage.FullName, fiImageBack.FullName);
                            Console.WriteLine($"scanPHNIResult.Success: {scanPHNIResult.Success}");
                            Console.WriteLine($"scanPHNIResult.Error: {scanPHNIResult.Error}");
                            Console.WriteLine($"scanPHNIResult.lastNameOrFullName: {scanPHNIResult.lastNameOrFullName}");
                            Console.WriteLine($"scanPHNIResult.documentNumber: {scanPHNIResult.documentNumber}");
                            Console.WriteLine($"scanPHNIResult.nationality: {scanPHNIResult.nationality}");
                            Console.WriteLine($"scanPHNIResult.documentIssueDate: {scanPHNIResult.documentIssueDate}");
                            Console.WriteLine($"scanPHNIResult.documentExpirationDate: {scanPHNIResult.documentExpirationDate}");
                            Console.WriteLine($"scanPHNIResult.addressLine1: {scanPHNIResult.addressLine1}");
                            Console.WriteLine($"scanPHNIResult.addressLine2: {scanPHNIResult.addressLine2}");
                            Console.WriteLine($"scanPHNIResult.postcode: {scanPHNIResult.postcode}");
                            ScanPHNIResult? dataExpected = JsonSerializer.Deserialize<ScanPHNIResult>(expectedDataJson, jsonOptions);
                            System.Diagnostics.Debug.WriteLine("scanPHNIResult: " + scanPHNIResult);
                            isValid = IsScanPHNIResultValid(scanPHNIResult, dataExpected);
                            Console.WriteLine(" --> IsValid: " + isValid);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ScanPHNI exception: {ex}");
                    }
                    break;
                default:
                    Console.WriteLine($"Unexpected documentType: {expectedDocumentType}");
                    break;
            }
            break;
        default:
            Console.WriteLine($"Unexpected country: {expectedCountry}");
            break;
    }

    /*
        https://localhost/csekycwebapiazfacedebug/ScanTextFromIDImageFileWithPrompt
        https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt
        https://localhost/csekycwebapiazface/ScanTextFromIDImageFile
     */
    //Console.WriteLine("try ScanTextFromIDImageFile...");
    //OCRIDImageByCloud("https://localhost/csekycwebapiazface/ScanTextFromIDImageFile", fiImage, fiImageBack, expectedDataJson);
    //Console.WriteLine("try ScanTextFromIDImageFileWithPrompt...");
    //OCRIDImageByCloud("https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt", fiImage, fiImageBack, expectedDataJson);
}
#endif

void OCRIDImageByCloud(string webapiUrl, FileInfo fiImage, FileInfo fiImageBack, string expectedDataJson)
{
    /////////////
    bool isValid = false;
    StreamContent fileImage = new StreamContent(fiImage.OpenRead());
    MultipartFormDataContent form = new MultipartFormDataContent();
    switch (fiImage.Extension.ToLower())
    {
        case ".pdf":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            break;
        case ".jpg":
        case ".jpeg":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            break;
        case ".png":
            fileImage.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            break;
        default:
            throw new Exception($"File type not supported.[{fiImage.Extension}]");
            break;
    }
    form.Add(fileImage, "idImage", fiImage.Name);

    if (fiImageBack != null)
    {
        StreamContent fileImageBack = new StreamContent(fiImageBack.OpenRead());
        switch (fiImageBack.Extension.ToLower())
        {
            case ".pdf":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
                break;
            case ".jpg":
            case ".jpeg":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                break;
            case ".png":
                fileImageBack.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                break;
            default:
                throw new Exception($"File type not supported.[{fiImageBack.Extension}]");
                break;
        }
        form.Add(fileImageBack, "idImageBack", fiImageBack.Name);
    }

    Console.Write($"{fiImage.Name} : ");

    // post
    string resultJson = "";
    using (var httpClient = new HttpClient() { Timeout = TimeSpan.FromMinutes(5) })
    {
        DateTime dtStart = DateTime.Now;
        //var response = httpClient.PostAsync($"https://localhost/csekycwebapiazfacedebug/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        //var response = httpClient.PostAsync($"https://localhost/csekycwebapiazface/ScanTextFromIDImageFileWithPrompt", form).GetAwaiter().GetResult();
        var response = httpClient.PostAsync(webapiUrl, form).GetAwaiter().GetResult();
        DateTime dtEnd = DateTime.Now;
        var elapsed = dtEnd - dtStart;
        Console.Write(" Elapsed: " + elapsed);
        System.Diagnostics.Debug.WriteLine("StatusCode: " + response.StatusCode);
        System.Diagnostics.Debug.WriteLine("Headers: " + response.Headers);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            System.Diagnostics.Debug.WriteLine("Content: " + response.Content);
            throw new Exception(response.ReasonPhrase);
        }

        if (response.Content != null)
        {
            resultJson = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("Content: " + resultJson);

            JObject jObjectResult = JObject.Parse(resultJson);
            if (jObjectResult["documentType"] != null)
            {
                string country = jObjectResult["country"].ToString();
                string documentType = jObjectResult["documentType"].ToString();
                Console.Write($" {country} : {documentType} : ");
                //using (StreamReader sr = new StreamReader(fiData.OpenRead()))
                {
                    //string expectedDataJson = sr.ReadToEnd();
                    switch (country)
                    {
                        /*
MYDL_TZ1145051_Test1.jpg :  Elapsed: 00:00:27.4395177 MY : DL :  --> IsValid: True
MYDL_S10701105740_Test1.jpg :  Elapsed: 00:00:19.2677005 MY : DL :  --> IsValid: True
MYKAD_730818-13-5230_Test1.jpg :  Elapsed: 00:00:10.0879668 MY : MY :   Error : Unexpected documentType [MY]
PhilID_2031-8439-2460-5291_Test1.jpg :  Elapsed: 00:00:08.6591035 PH : NI :   Error : Unexpected country [PH]
CRN-0111-8184420-7_Test1.jpg :  Elapsed: 00:00:15.7811114 PH : UI :   Error : Unexpected country [PH]
CRN-0111-2588996-9_Test1.jpg :  Elapsed: 00:00:15.2136485 PH : UI :   Error : Unexpected country [PH]
CRN-0033-5508686-0_Test1.jpg :  Elapsed: 00:00:08.6318526 PH : UI :   Error : Unexpected country [PH]
                         */
                        case "MY":
                            if (documentType == "DL")
                            {
                                ScanMYDLResult? dataExpected = JsonSerializer.Deserialize<ScanMYDLResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanMYDLResult? result = JsonSerializer.Deserialize<ScanMYDLResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanIDDLResult: " + result);
                                    isValid = IsScanMYDLResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine("  Error : Data expected is null.");
                                }
                            }
                            else if (documentType == "MY")
                            {
                                ScanMyKadResult? dataExpected = JsonSerializer.Deserialize<ScanMyKadResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanMyKadResult? result = JsonSerializer.Deserialize<ScanMyKadResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanMyKadResult: " + result);
                                    isValid = IsScanMyKadResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine("  Error : Data expected is null.");
                                }
                            }
                            else
                            {
                                ScanIDResult? result = JsonSerializer.Deserialize<ScanIDResult>(resultJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("ScanIDResult: " + result);
                                Console.WriteLine($"  Error : Unexpected documentType [{documentType}]");
                            }
                            break;
                        case "PH":
                            if (documentType == "NI")
                            {
                                ScanPHNIResult? dataExpected = JsonSerializer.Deserialize<ScanPHNIResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanPHNIResult? result = JsonSerializer.Deserialize<ScanPHNIResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanPHNIResult: " + result);
                                    isValid = IsScanPHNIResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine($"  Error : Data expected is null.");
                                }
                            }
                            else if (documentType == "UI")
                            {
                                ScanPHUMIDResult? dataExpected = JsonSerializer.Deserialize<ScanPHUMIDResult>(expectedDataJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("dataExpected: " + dataExpected);
                                if (dataExpected != null)
                                {
                                    ScanPHUMIDResult? result = JsonSerializer.Deserialize<ScanPHUMIDResult>(resultJson, jsonOptions);
                                    System.Diagnostics.Debug.WriteLine("ScanMyKadResult: " + result);
                                    isValid = IsScanPHUMIDResultValid(result, dataExpected);
                                    Console.WriteLine(" --> IsValid: " + isValid);
                                }
                                else
                                {
                                    Console.WriteLine("  Error : Data expected is null.");
                                }
                            }
                            else
                            {
                                ScanIDResult? result = JsonSerializer.Deserialize<ScanIDResult>(resultJson, jsonOptions);
                                System.Diagnostics.Debug.WriteLine("ScanIDResult: " + result);
                                Console.WriteLine($"  Error : Unexpected documentType [{documentType}]");
                            }
                            break;
                        default:
                            Console.WriteLine($"  Error : Unexpected country [{country}]");
                            ScanIDResult? dataExpected_ = JsonSerializer.Deserialize<ScanIDResult>(expectedDataJson, jsonOptions);
                            break;
                    }
                }
            }
        }
    }
    /////////////
}

bool IsScanMYDLResultValid(ScanMYDLResult result, ScanMYDLResult expected)
{
    int countNotMatch = 0;

    if(result == null || expected == null)
    {
        return false;
    }

    if(result.lastNameOrFullName != expected.lastNameOrFullName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }

    if (result.documentNumber != expected.documentNumber)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality != expected.nationality)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.documentExpirationDate != expected.documentExpirationDate)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentExpirationDate: " + result.documentExpirationDate);
        Console.WriteLine("  Expected    : documentExpirationDate: " + expected.documentExpirationDate);
    }

    if (result.documentIssueDate != expected.documentIssueDate)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentIssueDate: " + result.documentIssueDate);
        Console.WriteLine("  Expected    : documentIssueDate: " + expected.documentIssueDate);
    }

    if (result.addressLine1 != expected.addressLine1)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2 != expected.addressLine2)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode != expected.postcode)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown != expected.addressTown)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    return (countNotMatch == 0) ? true : false;
}
bool IsScanMyKadResultValid(ScanMyKadResult result, ScanMyKadResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName != expected.lastNameOrFullName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }

    if (result.documentNumber != expected.documentNumber)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality != expected.nationality)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1 != expected.addressLine1)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2 != expected.addressLine2)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode != expected.postcode)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown != expected.addressTown)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender != expected.gender)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}
#if true
bool IsScanPHNIResultValid(ScanPHNIResult result, ScanPHNIResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName != expected.lastNameOrFullName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName != expected.firstName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName != expected.middleName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth != expected.dateOfBirth)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber != expected.documentNumber)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality != expected.nationality)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1 != expected.addressLine1)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2 != expected.addressLine2)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode != expected.postcode)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown != expected.addressTown)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender != expected.gender)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}
#endif

bool IsScanPHUMIDResultValid(ScanPHUMIDResult result, ScanPHUMIDResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName != expected.lastNameOrFullName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName != expected.firstName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName != expected.middleName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth != expected.dateOfBirth)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber != expected.documentNumber)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality != expected.nationality)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1 != expected.addressLine1)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2 != expected.addressLine2)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode != expected.postcode)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown != expected.addressTown)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender != expected.gender)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}

bool IsScanPHDLResultValid(ScanPHDLResult result, ScanPHDLResult expected)
{
    int countNotMatch = 0;

    if (result == null || expected == null)
    {
        return false;
    }

    if (result.lastNameOrFullName != expected.lastNameOrFullName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: lastNameOrFullName: " + result.lastNameOrFullName);
        Console.WriteLine("  Expected    : lastNameOrFullName: " + expected.lastNameOrFullName);
    }
    if (result.firstName != expected.firstName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: firstName: " + result.firstName);
        Console.WriteLine("  Expected    : firstName: " + expected.firstName);
    }
    if (result.middleName != expected.middleName)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: middleName: " + result.middleName);
        Console.WriteLine("  Expected    : middleName: " + expected.middleName);
    }

    if (result.dateOfBirth != expected.dateOfBirth)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: dateOfBirth: " + result.dateOfBirth);
        Console.WriteLine("  Expected    : dateOfBirth: " + expected.dateOfBirth);
    }

    if (result.documentNumber != expected.documentNumber)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: documentNumber: " + result.documentNumber);
        Console.WriteLine("  Expected    : documentNumber: " + expected.documentNumber);
    }

    if (result.nationality != expected.nationality)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: nationality: " + result.nationality);
        Console.WriteLine("  Expected    : nationality: " + expected.nationality);
    }

    if (result.addressLine1 != expected.addressLine1)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine1: " + result.addressLine1);
        Console.WriteLine("  Expected    : addressLine1: " + expected.addressLine1);
    }

    if (result.addressLine2 != expected.addressLine2)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressLine2: " + result.addressLine2);
        Console.WriteLine("  Expected    : addressLine2: " + expected.addressLine2);
    }

    if (result.postcode != expected.postcode)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: postcode: " + result.postcode);
        Console.WriteLine("  Expected    : postcode: " + expected.postcode);
    }

    if (result.addressTown != expected.addressTown)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: addressTown: " + result.addressTown);
        Console.WriteLine("  Expected    : addressTown: " + expected.addressTown);
    }

    if (result.gender != expected.gender)
    {
        countNotMatch++;
        Console.WriteLine($"\n[{countNotMatch}]");
        Console.WriteLine("  Scanned Data: gender: " + result.gender);
        Console.WriteLine("  Expected    : gender: " + expected.gender);
    }

    return (countNotMatch == 0) ? true : false;
}
