namespace Portfolio
{
    /// <summary>
    /// Provides a static repository of fiat currency codes. This class serves as a centralized point
    /// to manage and validate fiat currencies used throughout the application.
    /// </summary>
    public static class FiatCurrencies
    {
        /// <summary>
        /// Gets the list of all fiat currency codes.
        /// </summary>
        /// <value>
        /// The list of fiat currency codes used for financial transactions and validations.
        /// </value>
        public static string[] Codes { get; } = [
            "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF", "CNY", "SEK", "NZD",
            "MXN", "SGD", "HKD", "NOK", "KRW", "TRY", "RUB", "INR", "BRL", "ZAR",
            "AOA", "ARS", "BND", "BRL", "BZD", "CAD", "CHF", "CLP", "COP", "CRC",
            "CZK", "DJF", "DKK", "DOP", "EUR", "FJD", "FKP", "GBP", "GEL", "GTQ",
            "HKD", "HNL", "HUF", "IDR", "ILS", "INR", "ISK", "JPY", "KES", "KGS",
            "KMF", "KRW", "KZT", "MDL", "MGA", "MRU", "MWK", "MXN", "MYR", "NOK",
            "NZD", "OMR", "PEN", "PGK", "PHP", "PLN", "PYG", "RON", "RWF", "SBD",
            "SCR", "SEK", "SGD", "SRD", "STN", "SZL", "TJS", "TMT", "TOP", "TRY",
            "USD", "UYU", "VND", "XCD", "ZAR"
        ];
    }
}