using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class EmptyMoneyAmountConverter : ValueConverter<decimal, decimal?>
{
    public EmptyMoneyAmountConverter()
        : base(
            money => (money == 0) ? new decimal?() : money,
            value => value != null ? value.Value : 0)
    {
    }
}