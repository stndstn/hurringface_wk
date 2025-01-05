using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ScanID
{    public class ScanIDResult
    {
        public bool Success { get; set; }
        public string Error { get; set; }

        protected string _documentType;
        public string documentType { get { return _documentType; } }
        public bool isBackOfIDImage { get; set; } = false;

        public string lastNameOrFullName { get; set; }
        public string firstName { get; set; }
        public string middleName { get; set; }
        public string documentNumber { get; set; }
        public string nationality { get; set; }
        public string dateOfBirth { get; set; }
        public string placeOfBirth { get; set; }
        public string gender { get; set; }
        public string maritalStatus { get; set; }
        public string documentExpirationDate { get; set; }
        public string documentIssueDate { get; set; }
        public string addressLine1 { get; set; }
        public string addressLine2 { get; set; }
        public string addressTown { get; set; }
        public string postcode { get; set; }
        public string personalNumber { get; set; }

        protected string _country;
        public string country { get { return _country; } }

        public string faceImageBase64 { get; set; }

        public string extraData { get; set; }

        public string resultJsonStringOCR { get; set; }
        public string resultJsonStringImageLabeling { get; set; }

        public double? documentLandmarksProbabilityAvg { get; set; }
    }
    public class ScanMYDLResult : ScanIDResult
    {
        public ScanMYDLResult()
        {
            _documentType = "DL";
            _country = "MY";
        }
    }

    public class ScanMyKadResult : ScanIDResult
    {
        public ScanMyKadResult()
        {
            _documentType = "MY";
            _country = "MY";
            nationality = "MY";
        }
    }

    public class ScanPHUMIDResult : ScanIDResult
    {
        public ScanPHUMIDResult()
        {
            _documentType = "UI";
            _country = "PH";
            nationality = "PH";
        }
    }

    public class ScanPHNIResult : ScanIDResult
    {
        public ScanPHNIResult()
        {
            _documentType = "NI";
            _country = "PH";
            nationality = "PH";
        }
    }

    public class ScanPHNIBKResult : ScanPHNIResult
    {
        public string QRCodeData { get; set; } = "";
        public bool IsQRCodeDataValid { get; set; } = false;

        public string QRCode_DateIssued { get; set; } = "";
        public string QRCode_Issuer { get; set; } = "";
        public string QRCode_alg { get; set; } = "";
        public string QRCode_signature { get; set; } = "";
        public string QRCode_subject_Suffix { get; set; } = "";
        public string QRCode_subject_lName { get; set; } = "";
        public string QRCode_subject_fName { get; set; } = "";
        public string QRCode_subject_mName { get; set; } = "";
        public string QRCode_subject_sex { get; set; } = "";
        public string QRCode_subject_BT { get; set; } = "";
        public string QRCode_subject_DOB { get; set; } = "";
        public string QRCode_subject_POB { get; set; } = "";
        public string QRCode_subject_PCN { get; set; } = "";

        public ScanPHNIBKResult()
        {
            isBackOfIDImage = true;
        }
        public ScanPHNIBKResult(ScanPHNIResult refData)
        {
            if (refData != null)
            {
                this.addressLine1 = refData.addressLine1;
                this.addressLine2 = refData.addressLine2;
                this.addressTown = refData.addressTown;
                this.dateOfBirth = refData.dateOfBirth;
                this.documentExpirationDate = refData.documentExpirationDate;
                this.documentNumber = refData.documentNumber;
                this.gender = refData.gender;
                this.maritalStatus = refData.maritalStatus;
                this.nationality = refData.nationality;
                this.personalNumber = refData.personalNumber;
                this.placeOfBirth = refData.placeOfBirth;
                this.postcode = refData.postcode;
                this.resultJsonStringImageLabeling = refData.resultJsonStringImageLabeling;
                this.resultJsonStringOCR = refData.resultJsonStringOCR;
                this.Success = refData.Success;
                if (refData is ScanPHNIBKResult)
                {
                    ScanPHNIBKResult back = (ScanPHNIBKResult)refData;
                    this.QRCodeData = back.QRCodeData;
                    this.IsQRCodeDataValid = back.IsQRCodeDataValid;
                    this.QRCode_alg = back.QRCode_alg;
                    this.QRCode_DateIssued = back.QRCode_DateIssued;
                    this.QRCode_Issuer = back.QRCode_Issuer;
                    this.QRCode_signature = back.QRCode_signature;
                    this.QRCode_subject_BT = back.QRCode_subject_BT;
                    this.QRCode_subject_DOB = back.QRCode_subject_DOB;
                    this.QRCode_subject_POB = back.QRCode_subject_POB;
                    this.QRCode_subject_PCN = back.QRCode_subject_PCN;
                    this.QRCode_subject_Suffix = back.QRCode_subject_Suffix;
                    this.QRCode_subject_lName = back.QRCode_subject_lName;
                    this.QRCode_subject_fName = back.QRCode_subject_fName;
                    this.QRCode_subject_mName = back.QRCode_subject_mName;
                    this.QRCode_subject_sex = back.QRCode_subject_sex;
                }
            }

            isBackOfIDImage = true;
        }
    }

    public class ScanPHDLResult : ScanIDResult
    {
        public ScanPHDLResult()
        {
            _documentType = "DL";
            _country = "PH";
        }
    }

    public class ScanIDeKTPResult : ScanIDResult
    {
        public string provinsi { get; set; }
        public string kabKota { get; set; }
        public string alamat { get; set; }
        public string rt { get; set; }
        public string rw { get; set; }
        public string kelDesa { get; set; }
        public string kecamatan { get; set; }

        public ScanIDeKTPResult()
        {
            _documentType = "KT";
            _country = "ID";
        }
    }

    public class ScanIDDLResult : ScanIDResult
    {
        public ScanIDDLResult()
        {
            _documentType = "DL";
            _country = "ID";
        }
    }

    public class ScanPassportMRZResult : ScanIDResult
    {
        public ScanPassportMRZResult()
        {
            _documentType = "PP";
        }

        public void SetCountry(Code.Country country)
        {
            _country = country.ncode;
        }
    }


    public class Line
    {
        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        public Line()
        {
        }

        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        /// <param name="boundingBox">Bounding box of a recognized
        /// line.</param>
        /// <param name="text">The text content of the line.</param>
        public Line(IList<double?> boundingBox, string text)
        {
            BoundingBox = boundingBox;
            if(boundingBox.Count == 8)
            {
                Baseline = new List<double?>();
                Baseline.Add(boundingBox[6]);   // X1 Left Bottom X
                Baseline.Add(boundingBox[7]);   // Y1 Left Bottom Y
                Baseline.Add(boundingBox[4]);   // X2 Right Bottom X
                Baseline.Add(boundingBox[5]);   // Y2 Right Bottom Y
            }
            else if (boundingBox.Count == 4)
            {
                // N/A
            }
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        /// <param name="boundingBox">Bounding box of a recognized
        /// line.</param>
        /// <param name="text">The text content of the line.</param>
        /// <param name="baseline">Baseline of a recognized line.</param>
        public Line(IList<double?> boundingBox, string text, IList<double?> baseline)
        {
            BoundingBox = boundingBox;
            Baseline = baseline;
            Text = text;
        }

        /// <summary>
        /// Initializes a new instance of the Line class.
        /// </summary>
        /// <param name="boundingBox">Bounding box of a recognized
        /// line.</param>
        /// <param name="text">The text content of the line.</param>
        /// <param name="baseline">Baseline of a recognized line.</param>
        /// <param name="confidence">Confidence score of a recognized line. 0.0 to 1.0</param>
        public Line(IList<double?> boundingBox, string text, IList<double?> baseline, double? confidence)
        {
            BoundingBox = boundingBox;
            Baseline = baseline;
            Text = text;
            Confidence = confidence;
        }

        /// <summary>
        /// Gets or sets bounding box of a recognized line.
        /// </summary>
        [JsonProperty(PropertyName = "boundingBox")]
        public IList<double?> BoundingBox { get; set; }

        [JsonProperty(PropertyName = "baseline")]
        public IList<double?> Baseline { get; set; }

        /// <summary>
        /// Gets or sets the text content of the line.
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the confident score of the text content.
        /// </summary>
        [JsonProperty(PropertyName = "confidence")]
        public double? Confidence { get; set; }

    }

    public class LabelInfo
    {
        const string REGEX_ESCAPE_CHARS = ".+*?^$()[]{}|\\";

        public LabelInfo(string title)
        {
            _title = title;
        }
        string _title;
        Line _line;
        IList<double?> _boundingBox = new List<double?>();
        public string Title { get { return _title; } }
        double? _left;
        public double? Left { get { return _left; } }
        double? _top;
        public double? Top { get { return _top; } }
        public double? Right { get { return _left + _width; } }
        double? _bottom;
        public double? Bottom { get { return _bottom; } }
        double? _height;
        public double? Height { get { return _height; } }
        double? _width;
        public double? Width { get { return _width; } }

        bool _isLabelFound = false;
        public bool IsLabelFound { get { return _isLabelFound; } }
        public Line LineMacthed { get { return _line; } }

        public bool MatchTitleExactly(Line line)
        {
            try
            {
                bool bRet = false;
                string text = line.Text.Trim();

                try
                {
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} Title: {Title}");
                    //if (line.Text.Trim().Replace('.', ' ').Replace(',', ' ').Trim() == Title.Trim().Replace('.', ' ').Replace(',', ' '))
                    // remove dot, comma, and blank character before compare
                    if (line.Text.Trim().Replace(".", "").Replace(",", "").Replace(" ", "").Trim() == Title.Trim().Replace(".", "").Replace(",", "").Replace(" ", ""))
                    {
                        bRet = true;
                        _isLabelFound = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
#if false
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
#else
                        _left = line.ExtGetLeft();
                        _top = line.ExtGetTop();
                        _bottom = line.ExtGetBottom();
#endif
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] Left:{Left} Top:{Top} Bottom:{Bottom}");
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
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTitleExactly [{line}] exception:{ex}");
                return false;
            }
        }
#if false
        public bool MatchTitle(Line line)
        {
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    string strC = c.ToString();
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(strC))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(strC))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (countCharNotIn < 3 && countCharIn > Title.Length - 3 && regexLine.Match(Title).Success)
                    {
                        bRet = true;
                        _isLabelFound = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
#if false
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
#else
                        _left = line.ExtGetLeft();
                        _top = line.ExtGetTop();
                        _bottom = line.ExtGetBottom();
#endif
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
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
                System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] MatchTitle [{line}] exception:{ex}");
                return false;
            }
        }
#else
        public bool MatchTitle(Line line)
        {
            return MatchTitleExactly(line);
        }

#endif
        /*
        public bool MatchTitle(Line line, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;
                string text = line.Text.Trim();
                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    bool bMatch = MatchWithSpellSuggestion(text, Title, spellSuggestion);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
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
        }
        */
        /*
        public bool MatchTitleWithSeparator(Line line, string separator, out string valueFollowedBySeparator)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();

                // remove text followed by separator
                string[] strings = text.Split(separator, 2);
                if (strings.Length != 2)
                    return false;

                if (string.IsNullOrEmpty(strings[0]))
                    return false;

                text = strings[0];

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);

                string strRegex = ".";
                foreach (char c in text)
                {
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(c))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && regexLine.Match(Title).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = strings[1].Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
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
        }
        public bool MatchTitleWithSeparator(Line line, string separator, out string valueFollowedBySeparator, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();

                // remove text followed by separator
                string[] strings = text.Split(separator, 2);
                if (strings.Length != 2)
                    return false;

                if (string.IsNullOrEmpty(strings[0]))
                    return false;

                text = strings[0];

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    bool bMatch = MatchWithSpellSuggestion(text, Title, spellSuggestion);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = strings[1].Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
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
        }
        */
        public bool MatchTitleFollowedByField(Line line, out string valueFollowedBySeparator)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();
                if (text.Length <= Title.Length)
                    return false;

                // extract same length as expected title
                text = text.Substring(0, Title.Length).Trim();

                string strRegex = ".";
                foreach (char c in text)
                {
                    string strC = c.ToString();
                    // regex escape char
                    //.+*?^$()[]{}|\
                    if (REGEX_ESCAPE_CHARS.Contains(strC))
                    {
                        strRegex += $"\\{c}?";
                    }
                    else
                    {
                        strRegex += $"{c}?";
                    }
                    if (!Title.Contains(strC))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }
                strRegex += ".";

                try
                {
                    Regex regexLine = new Regex(strRegex);
                    Match match = regexLine.Match(Title);
                    //System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, strRegex: {strRegex} Title: {Title}");
                    if (countCharNotIn < 3 && countCharIn > Title.Length - 3 && match.Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        valueFollowedBySeparator = line.Text.Substring(match.Value.Length).Trim();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        _line = line;
                        _boundingBox = line.BoundingBox;
#if false
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
#else
                        _left = line.ExtGetLeft();
                        _top = line.ExtGetTop();
                        _bottom = line.ExtGetBottom();
#endif
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
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
        }
        /*
        public bool MatchTitleFollowedByField(Line line, out string valueFollowedBySeparator, SpellSuggestionLib.SpellSuggestion spellSuggestion)
        {
            valueFollowedBySeparator = "";
            try
            {
                bool bRet = false;
                int countCharIn = 0;
                int countCharNotIn = 0;

                string text = line.Text.Trim();
                if (text.Length <= Title.Length)
                    return false;

                // extract same length as expected title
                text = text.Substring(0, Title.Length).Trim();

                double[] confidences = line.ExtGetConfidenceArray();
                Confidence confidence = new Confidence(confidences);
                foreach (char c in text)
                {
                    if (!Title.Contains(c))
                        countCharNotIn++;
                    else
                        countCharIn++;
                }

                try
                {
                    string wordSuggested = text;
                    bool bMatch = FindWithSpellSuggestion(text, Title, spellSuggestion, out wordSuggested);
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] confidence.avg: {confidence.Avg} countCharNotIn: {countCharNotIn}, countCharIn: {countCharIn}, Title: {Title}");
                    if (((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharNotIn == 0) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharNotIn < 3))
                     && ((confidence.Avg >= LABEL_CONFIDENCE_THRESHOLD && countCharIn == Title.Length) || (confidence.Avg > LABEL_CONFIDENCE_LOWER_THRESHOLD && countCharIn > Title.Length - 3))
                        && bMatch)
                    {
                        int pos = wordSuggested.IndexOf(Title);
                        if (pos >= 0)
                        {
                            if (pos + Title.Length < wordSuggested.Length)
                            {
                                valueFollowedBySeparator = wordSuggested.Substring(pos, Title.Length).Trim();
                            }
                            else
                            {
                                valueFollowedBySeparator = wordSuggested.Substring(pos).Trim();
                            }
                        }

                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {valueFollowedBySeparator}");
                        if (!Confidence.HasValue || Confidence.Value < confidences.Average())
                        {
                            _confidence = confidences.Average();
                            _line = line;
                            _boundingBox = line.BoundingBox;
                            _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                            _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                            _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
                            _height = line.ExtGetHeight();
                            _width = line.ExtGetWidth();
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Confidence:{Confidence} Left:{Left} Top:{Top} Bottom:{Bottom}");
                        }
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
        }
        */
        public bool MatchTitleRegex(Line line, String strPatternRegex)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] CheckCharInLine Title: {Title} line.Text {line.Text} strPatternRegex: {strPatternRegex}");
            try
            {
                bool bRet = false;
                string text = line.Text.Trim();

                try
                {
                    Regex regexLine = new Regex(strPatternRegex);
                    if (regexLine.Match(line.Text).Success)
                    {
                        bRet = true;
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] --> {Title}");
                        _isLabelFound = true;
                        _line = line;
                        _boundingBox = line.BoundingBox;
#if false
                        _left = (line.BoundingBox[0] + line.BoundingBox[6]) / 2.0;
                        _top = (line.BoundingBox[1] + line.BoundingBox[3]) / 2.0;
                        _bottom = (line.BoundingBox[7] + line.BoundingBox[5]) / 2.0;
#else
                        _left = line.ExtGetLeft();
                        _top = line.ExtGetTop();
                        _bottom = line.ExtGetBottom();
#endif
                        _height = line.ExtGetHeight();
                        _width = line.ExtGetWidth();
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}]     Left:{Left} Top:{Top} Bottom:{Bottom}");
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
        }

        public bool IsFieldInSameLeftEdge(Line field)
        {
            if (Math.Abs((double)(field.BoundingBox[0].Value - Left)) < Height * 3)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldJustUnderTheLabel(Line field)
        {
            if (Math.Abs((double)(field.BoundingBox[0].Value - Left)) < Height * 3
                && (double)(field.ExtGetTop() - Top) >= 0
                && (double)(field.ExtGetTop() - Bottom) < Height * 2)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldUnderTheLabel(Line field)
        {
            if ((double)(field.ExtGetTop() - Bottom) >= 0)
            {
                return true;
            }
            return false;
        }
        public bool IsFieldRightNextToTheLabel(Line field)
        {
            if (_boundingBox != null)
            {
                if (_boundingBox.Count >= 8)
                {
                    double dx = (double)((_boundingBox[4] - _boundingBox[6]) + (_boundingBox[2] - _boundingBox[0])) / 2.0;
                    double dy = (double)((_boundingBox[5] - _boundingBox[7]) + (_boundingBox[3] - _boundingBox[1])) / 2.0;
                    double a = 0;
                    if (dx != 0)
                    {
                        a = dy / dx;
                    }
                    // y = ax + b
                    // b = y - ax;
                    double b = (double)(_boundingBox[7] - (a * _boundingBox[6]));
                    if (field.BoundingBox != null && field.BoundingBox.Count >= 8)
                    {
                        double x1 = (double)field.BoundingBox[6];
                        double y1 = a * x1 + b;
                        double x2 = (double)field.BoundingBox[4];
                        double y2 = a * x2 + b;

                        if (Math.Abs((double)(field.BoundingBox[7] - y1)) < (Height) && Math.Abs((double)(field.BoundingBox[5] - y2)) < (Height))
                        {
                            return true;
                        }
                    }
                }
                else if (_boundingBox.Count == 4)
                {
                    // not able to calculate angle
                }
            }


            if (Right < field.ExtGetLeft()
                && Math.Abs((double)(field.ExtGetBottom() - Bottom)) < (Height / 2))
            {
                return true;
            }
            return false;
        }

    }

    static class Extension
    {
        /*
        public static int ExtGetLeftOfBox(this OcrLine ocrLine)
        {
            int left = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[0], out left);
            }
            return left;
        }
        public static int ExtGetTopOfBox(this OcrLine ocrLine)
        {
            int top = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[1], out top);
            }
            return top;
        }
        public static int ExtGetRightOfBox(this OcrLine ocrLine)
        {
            int right = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[2], out right);
            }
            return right;
        }
        public static int ExtGetBottomOfBox(this OcrLine ocrLine)
        {
            int bottom = 0;
            string[] values = ocrLine.BoundingBox.Split(',');
            if (values.Length == 4)
            {
                int.TryParse(values[3], out bottom);
            }
            return bottom;
        }
        public static string ExtGetText(this OcrLine ocrLine)
        {
            string ret = "";
            foreach (var word in ocrLine.Words)
            {
                if (!string.IsNullOrEmpty(ret))
                    ret += " ";
                ret += word.Text;
            }
            return ret;
        }
        public static string ExtGetConfidenceArrayToString(this Line line)
        {
            string ret = "";
            foreach (var word in line.Words)
            {
                if (string.IsNullOrEmpty(ret))
                    ret += "[";
                else
                    ret += ",";
                ret += word.Confidence;
            }
            ret += "]";
            return ret;
        }
        public static double[] ExtGetConfidenceArray(this Line line)
        {
            List<double> confidences = new List<double>();
            foreach (var word in line.Words)
            {
                confidences.Add(word.Confidence);
            }
            return confidences.ToArray();
        }
        */
        public static double? ExtGetHeight(this Line line)
        {
            if(line.BoundingBox.Count == 8)
            {
                //double? xLT = line.BoundingBox[0];
                double? yLT = line.BoundingBox[1];
                //double? xRT = line.BoundingBox[2];
                double? yRT = line.BoundingBox[3];
                //double? xRB = line.BoundingBox[4];
                double? yRB = line.BoundingBox[5];
                //double? xLB = line.BoundingBox[6];
                double? yLB = line.BoundingBox[7];
                double? h = ((yRB - yRT) + (yLB - yLT)) / 2;
                return h;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? yLT = line.BoundingBox[1];
                double? yRB = line.BoundingBox[3];
                if (line.Baseline != null && line.Baseline.Count == 4)
                {
                    double? yLB = line.Baseline[1]; // Y1
                    double? h = yLB - yLT;
                    return h;
                }
                else
                {
                    double? h = yRB - yLT;
                    return h;
                }
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }
        public static double? ExtGetWidth(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? xLT = line.BoundingBox[0];
                //double? yLT = line.BoundingBox[1];
                double? xRT = line.BoundingBox[2];
                //double? yRT = line.BoundingBox[3];
                double? xRB = line.BoundingBox[4];
                //double? yRB = line.BoundingBox[5];
                double? xLB = line.BoundingBox[6];
                //double? yLB = line.BoundingBox[7];
                double? w = ((xRB - xLB) + (xRT - xLT)) / 2;
                return w;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? xLT = line.BoundingBox[0];
                double? xRB = line.BoundingBox[2];
                double? w = xRB - xLT;
                return w;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }
        public static double? ExtGetBottom(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? yRB = line.BoundingBox[5];
                double? yLB = line.BoundingBox[7];
                double? bottom = (yRB + yLB) / 2;
                return bottom;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? yRB = line.BoundingBox[3];
                double? bottom = yRB;
                return bottom;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }
        public static double? ExtGetTop(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? yLT = line.BoundingBox[1];
                double? yRT = line.BoundingBox[3];
                double? top = (yLT + yRT) / 2;
                return top;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? yLT = line.BoundingBox[1];
                double? top = yLT;
                return top;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }
        public static double? ExtGetLeft(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? xLT = line.BoundingBox[0];
                double? xLB = line.BoundingBox[6];
                double? left = (xLT + xLB) / 2;
                return left;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? xLT = line.BoundingBox[0];
                double? left = xLT;
                return left;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }
        public static double? ExtGetRight(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? xRT = line.BoundingBox[2];
                double? xRB = line.BoundingBox[4];
                double? right = (xRT + xRB) / 2;
                return right;
            }
            else if (line.BoundingBox.Count == 4)
            {
                double? xRB = line.BoundingBox[2];
                double? right = xRB;
                return right;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }

        public static double? ExtGetAngle(this Line line)
        {
            if (line.BoundingBox.Count == 8)
            {
                double? xLT = line.BoundingBox[0];
                double? yLT = line.BoundingBox[1];
                double? xRT = line.BoundingBox[2];
                double? yRT = line.BoundingBox[3];
                double? xRB = line.BoundingBox[4];
                double? yRB = line.BoundingBox[5];
                double? xLB = line.BoundingBox[6];
                double? yLB = line.BoundingBox[7];
                if (xRB - xLB == 0)
                {
                    if (yRB - yLB > 0)
                        return 90;
                    else
                        return -90;
                }
                else
                {
                    double radian = Math.Atan((double)((yRB - yLB) / (xRB - xLB)));
                    double angle = radian * (180 / Math.PI);
                    return angle;
                }
            }
            else if (line.BoundingBox.Count == 4)
            {
                return 0;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is: {line.BoundingBox.Count}");
        }

        public static string ExtToString(this Line line)
        {
            string ret = "";
            ret += $"({line.ExtGetLeft().Value.ToString("F01").PadLeft(7, ' ')},";
            ret += $" {line.ExtGetTop().Value.ToString("F01").PadLeft(7, ' ')})-";
            ret += $"({line.ExtGetRight().Value.ToString("F01").PadLeft(7, ' ')},";
            ret += $" {line.ExtGetBottom().Value.ToString("F01").PadLeft(7, ' ')})";
            ret += $" {line.ExtGetWidth().Value.ToString("F01").PadLeft(7, ' ')} :";
            ret += $" {line.ExtGetHeight().Value.ToString("F01").PadLeft(7, ' ')}";
            ret += $" {line.Text} ({line.Confidence})";
            return ret;
        }

        public static double? ExtGetBaselineSlope(this Line line)
        {
            double? ret = null;
            if(line.Baseline != null && line.Baseline.Count == 4)
            {
                double dx = (double)(line.Baseline[2] - line.Baseline[0]);
                double dy = (double)(line.Baseline[3] - line.Baseline[1]);
                if(dx != 0)
                {
                    ret = dy / dx;
                }
                else
                {
                    ret = double.NaN;
                }
            }
            return ret;
        }
        public static double? ExtGetBaselineInterceptWithYAxis(this Line line)
        {
            double? b = null;
            if (line.Baseline != null && line.Baseline.Count == 4)
            {
                double? a = line.ExtGetBaselineSlope();
                if(a != null && !double.IsInfinity(a.Value) && !double.IsNaN(a.Value))
                {
                    double x1 = (double)line.Baseline[0];
                    double y1 = (double)line.Baseline[1];
                    double? b1 = (double)(y1 - a * x1);
                    double x2 = (double)line.Baseline[2];
                    double y2 = (double)line.Baseline[3];
                    double? b2 = (double)(y2 - a * x2);
                    b = (b1 + b2) / 2;
                }
            }
            return b;
        }
#if false
        public static Line MergedLine(this Line line1, Line line2)
        {
            if (line1.BoundingBox.Count == 8 && line2.BoundingBox.Count == 8)
            {
                List<double?> boundingBox = new List<double?>() {
                    line1.BoundingBox[0], line1.BoundingBox[1], //left top
                    line2.BoundingBox[2], line2.BoundingBox[3], //right top
                    line2.BoundingBox[4], line2.BoundingBox[5], //right bottom
                    line1.BoundingBox[6], line1.BoundingBox[7]  //left bottom
                };

                //string text = line1.Text + " " + line2.Text;
                string text = "";
                double dist = (double)(line2.ExtGetLeft() - line1.ExtGetRight());
                if (dist < line1.ExtGetHeight() / 4)
                {
                    // too close, no need to add blank space
                    text = line1.Text + line2.Text;
                }
                else
                {
                    text = line1.Text + " " + line2.Text;
                }
                System.Diagnostics.Debug.WriteLine($"MergedLine [{line1.Text}]-[{line2.Text}] dist: {dist} --> [{text}]");

                //List<Word> words = line1.Words.ToList();
                //words.AddRange(line2.Words);
                //Line newLine = new Line(boundingBox, text, words);
                Line newLine = new Line(boundingBox, text);
                return newLine;
            }
            else if (line1.BoundingBox.Count == 4 && line2.BoundingBox.Count == 4)
            {
                List<double?> boundingBox = new List<double?>() {
                    line1.BoundingBox[0], line1.BoundingBox[1], //left top
                    line2.BoundingBox[2], line2.BoundingBox[3], //right bottom
                };
                //string text = line1.Text + " " + line2.Text;
                string text = "";
                double dist = (double)(line2.ExtGetLeft() - line1.ExtGetRight());
                if (dist < line1.ExtGetHeight() / 4)
                {
                    // too close, no need to add blank space
                    text = line1.Text + line2.Text;
                }
                else
                {
                    text = line1.Text + " " + line2.Text;
                }
                System.Diagnostics.Debug.WriteLine($"MergedLine [{line1.Text}]-[{line2.Text}] dist: {dist} --> [{text}]");
                Line newLine = new Line(boundingBox, text);
                return newLine;
            }
            throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is (line1:{line1.BoundingBox.Count}) and (line2:{line2.BoundingBox.Count})");
        }
#else
        public static Line MergedLine(this Line line1, Line line2)
        {
            List<double?> boundingBox = null;
            if (line1.BoundingBox.Count == 8 && line2.BoundingBox.Count == 8)
            {
                boundingBox = new List<double?>() {
                    line1.BoundingBox[0], line1.BoundingBox[1], //left top
                    line2.BoundingBox[2], line2.BoundingBox[3], //right top
                    line2.BoundingBox[4], line2.BoundingBox[5], //right bottom
                    line1.BoundingBox[6], line1.BoundingBox[7]  //left bottom
                };
            }
            else if (line1.BoundingBox.Count == 4 && line2.BoundingBox.Count == 4)
            {
                boundingBox = new List<double?>() {
                    line1.BoundingBox[0], line1.BoundingBox[1], //left top
                    line2.BoundingBox[2], line2.BoundingBox[3], //right bottom
                };
            }
            else
            {
                throw new Exception($"line.BoundingBox.Count should be 4 or 8. It is (line1:{line1.BoundingBox.Count}) and (line2:{line2.BoundingBox.Count})");
            }
            string text = "";
            double dist = (double)(line2.ExtGetLeft() - line1.ExtGetRight());
            double widthBlank = (double)(line1.ExtGetHeight() / 2);
            int numBlankChar = (int)(dist / widthBlank);
            text = line1.Text;
            for (int i = 0; i < numBlankChar; i++)
            {
                text += " ";
            }
            text += line2.Text;
            System.Diagnostics.Debug.WriteLine($"MergedLine [{line1.Text}]-[{line2.Text}] dist: {dist} --> [{text}]");

            //List<Word> words = line1.Words.ToList();
            //words.AddRange(line2.Words);
            //Line newLine = new Line(boundingBox, text, words);
            Line newLine = new Line(boundingBox, text);
            return newLine;
        }
#endif
    }
}
