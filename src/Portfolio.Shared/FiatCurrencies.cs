using CSharpFunctionalExtensions;

namespace Portfolio.Shared
{
    /// <summary>
    /// Represents a fiat currency code as a value object.
    /// </summary>
    public class FiatCurrency : EnumValueObject<FiatCurrency>
    {
        public static readonly FiatCurrency USD = new FiatCurrency("USD");
        public static readonly FiatCurrency EUR = new FiatCurrency("EUR");
        public static readonly FiatCurrency JPY = new FiatCurrency("JPY");
        public static readonly FiatCurrency GBP = new FiatCurrency("GBP");
        public static readonly FiatCurrency AUD = new FiatCurrency("AUD");
        public static readonly FiatCurrency CAD = new FiatCurrency("CAD");
        public static readonly FiatCurrency CHF = new FiatCurrency("CHF");
        public static readonly FiatCurrency CNY = new FiatCurrency("CNY");
        public static readonly FiatCurrency SEK = new FiatCurrency("SEK");
        public static readonly FiatCurrency NZD = new FiatCurrency("NZD");
        public static readonly FiatCurrency MXN = new FiatCurrency("MXN");
        public static readonly FiatCurrency SGD = new FiatCurrency("SGD");
        public static readonly FiatCurrency HKD = new FiatCurrency("HKD");
        public static readonly FiatCurrency NOK = new FiatCurrency("NOK");
        public static readonly FiatCurrency KRW = new FiatCurrency("KRW");
        public static readonly FiatCurrency TRY = new FiatCurrency("TRY");
        public static readonly FiatCurrency RUB = new FiatCurrency("RUB");
        public static readonly FiatCurrency INR = new FiatCurrency("INR");
        public static readonly FiatCurrency BRL = new FiatCurrency("BRL");
        public static readonly FiatCurrency ZAR = new FiatCurrency("ZAR");
        public static readonly FiatCurrency AOA = new FiatCurrency("AOA");
        public static readonly FiatCurrency ARS = new FiatCurrency("ARS");
        public static readonly FiatCurrency BND = new FiatCurrency("BND");
        public static readonly FiatCurrency BZD = new FiatCurrency("BZD");
        public static readonly FiatCurrency CLP = new FiatCurrency("CLP");
        public static readonly FiatCurrency COP = new FiatCurrency("COP");
        public static readonly FiatCurrency CRC = new FiatCurrency("CRC");
        public static readonly FiatCurrency CZK = new FiatCurrency("CZK");
        public static readonly FiatCurrency DJF = new FiatCurrency("DJF");
        public static readonly FiatCurrency DKK = new FiatCurrency("DKK");
        public static readonly FiatCurrency DOP = new FiatCurrency("DOP");
        public static readonly FiatCurrency FJD = new FiatCurrency("FJD");
        public static readonly FiatCurrency FKP = new FiatCurrency("FKP");
        public static readonly FiatCurrency GEL = new FiatCurrency("GEL");
        public static readonly FiatCurrency GTQ = new FiatCurrency("GTQ");
        public static readonly FiatCurrency HNL = new FiatCurrency("HNL");
        public static readonly FiatCurrency HUF = new FiatCurrency("HUF");
        public static readonly FiatCurrency IDR = new FiatCurrency("IDR");
        public static readonly FiatCurrency ILS = new FiatCurrency("ILS");
        public static readonly FiatCurrency ISK = new FiatCurrency("ISK");
        public static readonly FiatCurrency KES = new FiatCurrency("KES");
        public static readonly FiatCurrency KGS = new FiatCurrency("KGS");
        public static readonly FiatCurrency KMF = new FiatCurrency("KMF");
        public static readonly FiatCurrency KZT = new FiatCurrency("KZT");
        public static readonly FiatCurrency MDL = new FiatCurrency("MDL");
        public static readonly FiatCurrency MGA = new FiatCurrency("MGA");
        public static readonly FiatCurrency MRU = new FiatCurrency("MRU");
        public static readonly FiatCurrency MWK = new FiatCurrency("MWK");
        public static readonly FiatCurrency MYR = new FiatCurrency("MYR");
        public static readonly FiatCurrency OMR = new FiatCurrency("OMR");
        public static readonly FiatCurrency PEN = new FiatCurrency("PEN");
        public static readonly FiatCurrency PGK = new FiatCurrency("PGK");
        public static readonly FiatCurrency PHP = new FiatCurrency("PHP");
        public static readonly FiatCurrency PLN = new FiatCurrency("PLN");
        public static readonly FiatCurrency PYG = new FiatCurrency("PYG");
        public static readonly FiatCurrency RON = new FiatCurrency("RON");
        public static readonly FiatCurrency RWF = new FiatCurrency("RWF");
        public static readonly FiatCurrency SBD = new FiatCurrency("SBD");
        public static readonly FiatCurrency SCR = new FiatCurrency("SCR");
        public static readonly FiatCurrency SRD = new FiatCurrency("SRD");
        public static readonly FiatCurrency STN = new FiatCurrency("STN");
        public static readonly FiatCurrency SZL = new FiatCurrency("SZL");
        public static readonly FiatCurrency TJS = new FiatCurrency("TJS");
        public static readonly FiatCurrency TMT = new FiatCurrency("TMT");
        public static readonly FiatCurrency TOP = new FiatCurrency("TOP");
        public static readonly FiatCurrency UYU = new FiatCurrency("UYU");
        public static readonly FiatCurrency VND = new FiatCurrency("VND");
        public static readonly FiatCurrency XCD = new FiatCurrency("XCD");

        private FiatCurrency(string code) : base(code) { }
    }
}
