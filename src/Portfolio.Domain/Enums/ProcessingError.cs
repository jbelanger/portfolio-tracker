namespace Portfolio.Domain.ValueObjects;

public enum ErrorType
{
    None, // No error
    PriceHistoryUnavailable, // Price history could not be retrieved
    InsufficientFunds, // Attempted to trade or withdraw more than available
    InvalidCurrency, // Currency code not recognized
    DataCorruption, // General data issues, possibly due to external factors
    ManualReviewRequired, // Generic case where user needs to inspect the transaction
    EventCreationFailed // When error creating tax event
}
