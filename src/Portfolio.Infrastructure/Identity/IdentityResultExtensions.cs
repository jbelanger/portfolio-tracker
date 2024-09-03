﻿using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;

namespace Portfolio.Infrastructure.Identity;

public static class IdentityResultExtensions
{
    public static Result ToApplicationResult(this IdentityResult result)
    {
        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join(", ",result.Errors.Select(e => e.Description)));
    }
}
