using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ScanID
{
    public class Code
    {
		public class Country
		{
			public string ncode { get; set; } = "";
            public string ncode3 { get; set; } = "";
			public string nname { get; set; } = "";
        }

		static public Country FindCountryBy3LetterCode(string ncode3)
		{
			Country country = null;

			foreach (var c in Countries)
			{
				if (c.ncode3.ToUpper() == ncode3.ToUpper())
				{
					country = c;
					break;
				}
			}
			return country;
		}

        static public Country FindCountryBy2LetterCode(string ncode)
        {
            Country country = null;

            foreach (var c in Countries)
            {
                if (c.ncode.ToUpper() == ncode.ToUpper())
                {
                    country = c;
                    break;
                }
            }
            return country;
        }

        static public Country[] Countries = {
			new Country { ncode = "AF", ncode3 = "AFG", nname= "AFGHANISTAN" },
			new Country { ncode = "AU", ncode3 = "AUS", nname= "AUSTRALIA" },
			new Country { ncode = "FR", ncode3 = "FRA", nname= "FRANCE" },
			new Country { ncode = "ID", ncode3 = "IDN", nname= "INDONESIA" },
			new Country { ncode = "IR", ncode3 = "IRN", nname= "IRAN" },
			new Country { ncode = "JP", ncode3 = "JPN", nname= "JAPAN" },
			new Country { ncode = "MY", ncode3 = "MYS", nname= "MALAYSIA" },
			new Country { ncode = "MX", ncode3 = "MEX", nname= "MEXICO" },
			new Country { ncode = "PH", ncode3 = "PHL", nname= "PHILIPPINES" },
			new Country { ncode = "SG", ncode3 = "SGP", nname= "SINGAPORE" },
			new Country { ncode = "TH", ncode3 = "THA", nname= "THAILAND" },
			new Country { ncode = "GB", ncode3 = "GBR", nname= "UNITED KINGDOM" },
			new Country { ncode = "US", ncode3 = "USA", nname= "UNITED STATES OF AMERICA" },
			new Country { ncode = "VN", ncode3 = "VNM", nname= "VIETNAM" },
            new Country { ncode = "ZZ", ncode3 = "ZZZ", nname= "UNKNOWN" }
        };
    }
}

/*
var theCountries = [
	new Country { ncode= "AF", ncode3 = "AFG", nname= "AFGHANISTAN" },
	new Country { ncode= "AU", ncode3 = "AUS", nname= "AUSTRALIA" },
	new Country { ncode= "FR", ncode3 = "FRA", nname= "FRANCE" },
	new Country { ncode= "ID", ncode3 = "IDN", nname= "INDONESIA" },
	new Country { ncode= "IR", ncode3 = "IRN", nname= "IRAN" },
	new Country { ncode= "JP", ncode3 = "JPN", nname= "JAPAN" },
	new Country { ncode= "MY", ncode3 = "MYS", nname= "MALAYSIA" },
	new Country { ncode= "MX", ncode3 = "MEX", nname= "MEXICO" },
	new Country { ncode= "PH", ncode3 = "PHL", nname= "PHILIPPINES" },
	new Country { ncode= "SG", ncode3 = "SGP", nname= "SINGAPORE" },
	new Country { ncode= "TH", ncode3 = "THA", nname= "THAILAND" },
	new Country { ncode= "GB", ncode3 = "GBR", nname= "UNITED KINGDOM" },
	new Country { ncode= "US", ncode3 = "USA", nname= "UNITED STATES OF AMERICA" },
	new Country { ncode= "VN", ncode3 = "VNM", nname= "VIETNAM' }
];
 */