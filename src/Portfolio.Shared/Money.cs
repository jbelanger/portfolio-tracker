namespace Portfolio
{
    /// <summary>
    /// Represents a monetary amount in a specific currency, ensuring proper handling
    /// of operations between amounts in the same or different currencies.
    /// </summary>
    public record Money(decimal Amount, string CurrencyCode)
    {
        /// <summary>
        /// Checks if the currency of this money instance is among predefined fiat currencies.
        /// </summary>
        /// <value>
        /// True if the currency is a fiat currency; otherwise, false.
        /// </value>
        public bool IsFiatCurrency => FiatCurrencies.Codes.Any(c => c == CurrencyCode);

        /// <summary>
        /// Gets the absolute value of the monetary amount.
        /// </summary>
        /// <value>
        /// The absolute (non-negative) value of the Amount.
        /// </value>
        public decimal AbsoluteAmount => Math.Abs(Amount);

        /// <summary>
        /// Converts the amount to a new Money instance with an absolute value of the amount.
        /// This ensures the monetary value is non-negative while retaining the original currency.
        /// </summary>
        /// <returns>A new Money instance representing the absolute value of the current instance.</returns>
        public Money ToAbsoluteAmountMoney()
        {
            return new Money(AbsoluteAmount, CurrencyCode);
        }

        /// <summary>
        /// Adds a specified Money instance to this instance and returns the result.
        /// The operation is only valid if both money instances have the same currency.
        /// If the 'other' Money instance is null, this instance is returned.
        /// </summary>
        /// <param name="other">The Money instance to add to this instance.</param>
        /// <returns>A new Money instance representing the sum of both amounts, or this instance if 'other' is null.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the currency codes do not match.</exception>
        public Money Add(Money? other)
        {
            if (other == null)
                return this;

            if (this.CurrencyCode != other.CurrencyCode)
                throw new InvalidOperationException("Cannot add amounts in different currencies.");

            return new Money(this.Amount + other.Amount, this.CurrencyCode);
        }

        /// <summary>
        /// Subtracts a specified Money instance from this instance and returns the result.
        /// The operation is only valid if both money instances have the same currency.
        /// If the 'other' Money instance is null, this instance is returned.
        /// </summary>
        /// <param name="other">The Money instance to subtract from this instance.</param>
        /// <returns>A new Money instance representing the difference of both amounts, or this instance if 'other' is null.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the currency codes do not match.</exception>
        public Money Subtract(Money? other)
        {
            if (other == null)
                return this;

            if (this.CurrencyCode != other.CurrencyCode)
                throw new InvalidOperationException("Cannot subtract amounts in different currencies.");

            return new Money(this.Amount - other.Amount, this.CurrencyCode);
        }

        /// <summary>
        /// Converts the Money instance to a string representation, typically for display or logging.
        /// </summary>
        /// <returns>A string that represents the current Money instance.</returns>
        public override string ToString()
        {
            return $"{Amount:0.##} {CurrencyCode}";
        }
    }
}
