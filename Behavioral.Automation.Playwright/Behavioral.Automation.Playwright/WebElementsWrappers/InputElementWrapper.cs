﻿using System.Threading.Tasks;
using Behavioral.Automation.Playwright.Context;
using Behavioral.Automation.Playwright.WebElementsWrappers.Interface;
using JetBrains.Annotations;
using Microsoft.Playwright;

namespace Behavioral.Automation.Playwright.WebElementsWrappers;

public class InputElementWrapper: WebElementWrapper, IInputWebElement
{
    public InputElementWrapper([NotNull] WebContext webContext, [NotNull] ILocator locator, [NotNull] string caption) : base(webContext, locator, caption)
    {
    }

    public async Task TypeAsync(string text)
    {
        await Locator.TypeAsync(text);
    }
}