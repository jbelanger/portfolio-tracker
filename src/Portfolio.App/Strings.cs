namespace Portfolio.App;

public class Strings
{
    public const string DATE_FORMAT = "yyyy-MM-dd";
    public const string CURRENCY_USD = "USD";
}

public class Errors
{
    public const string ERR_SAME_SYMBOLS = "Symbols must be of different currency/coin ({0}-{1}).";
    public const string ERR_YAHOO_API_FETCH_FAILURE = "An error occurred while fetching from Yahoo Finance API.";
}