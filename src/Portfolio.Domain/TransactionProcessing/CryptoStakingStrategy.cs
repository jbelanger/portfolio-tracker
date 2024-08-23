// using CSharpFunctionalExtensions;
// using Portfolio.Domain.Entities;
// using Portfolio.Domain.ValueObjects;

// namespace Portfolio.Domain;

// public class CryptoStakingStrategy : ITransactionStrategy
// {
//     private readonly decimal _stakingRewardRate; // Annual reward rate in percentage (e.g., 5% = 0.05)

//     public CryptoStakingStrategy(decimal stakingRewardRate)
//     {
//         _stakingRewardRate = stakingRewardRate;
//     }

//     public async Task<Result> ProcessTransactionAsync(CryptoCurrencyRawTransaction tx, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
//     {
//         var holding = portfolio.GetOrCreateHolding(tx.ReceivedAmount.CurrencyCode);

//         if (tx.Type == TransactionType.Stake)
//         {
//             return Stake(tx, holding);
//         }
//         else if (tx.Type == TransactionType.Unstake)
//         {
//             return Unstake(tx, holding, portfolio);
//         }
//         else if (tx.Type == TransactionType.ClaimRewards)
//         {
//             return await ClaimRewards(tx, holding, portfolio, priceHistoryService);
//         }
//         else
//         {
//             return Result.Failure("Invalid staking transaction type.");
//         }
//     }

//     private Result Stake(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding holding)
//     {
//         if (!TransactionValidationUtils.EnsureAboveZeroAmount(tx)) return Result.Failure(tx.ErrorMessage);

//         holding.StakedBalance += tx.ReceivedAmount.Amount;
//         holding.Balance -= tx.ReceivedAmount.Amount;

//         TransactionValidationUtils.EnsureBalanceNotNegative(tx, holding.Asset, holding.Balance);
//         return Result.Success();
//     }

//     private Result Unstake(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding holding, UserPortfolio portfolio)
//     {
//         if (tx.ReceivedAmount.Amount > holding.StakedBalance)
//         {
//             return Result.Failure("Cannot unstake more than the staked balance.");
//         }

//         holding.StakedBalance -= tx.ReceivedAmount.Amount;
//         holding.Balance += tx.ReceivedAmount.Amount;

//         // Potentially create a taxable event if there's a change in value
//         // Since unstaking itself is generally not a sale, no taxable event is created here.
//         // However, in some jurisdictions, moving from staked to unstaked could be considered a taxable event if the asset's value changes.

//         return Result.Success();
//     }

//     private async Task<Result> ClaimRewards(CryptoCurrencyRawTransaction tx, CryptoCurrencyHolding holding, UserPortfolio portfolio, IPriceHistoryService priceHistoryService)
//     {
//         var rewards = CalculateStakingRewards(holding);
//         holding.Balance += rewards;

//         // Create a taxable event for the staking rewards
//         var rewardPriceResult = await priceHistoryService.GetPriceAtCloseTimeAsync(holding.Asset, tx.DateTime);
//         if (rewardPriceResult.IsSuccess)
//         {
//             var taxableEventResult = TaxableEvent.Create(tx.DateTime, holding.Asset, 0, rewardPriceResult.Value, rewards, portfolio.DefaultCurrency);
//             if (taxableEventResult.IsSuccess)
//             {
//                 portfolio.AddTaxableEvent(taxableEventResult.Value);
//             }
//             else
//             {
//                 tx.ErrorMessage = "Could not create taxable event for staking rewards.";
//                 tx.ErrorType = ErrorType.TaxEventNotCreated;
//             }
//         }
//         else
//         {
//             tx.ErrorType = ErrorType.PriceHistoryUnavailable;
//             tx.ErrorMessage = $"Could not get price history for {holding.Asset}. Rewards taxable event will be incorrect.";
//         }

//         return Result.Success();
//     }

//     private decimal CalculateStakingRewards(CryptoCurrencyHolding holding)
//     {
//         // Simplified reward calculation (annualized rewards distributed daily)
//         decimal annualRewards = holding.StakedBalance * _stakingRewardRate;
//         decimal dailyRewards = annualRewards / 365;

//         // In a real-world scenario, you might track the exact staking period
//         // and adjust rewards based on the actual staking duration.

//         return dailyRewards;
//     }
// }
