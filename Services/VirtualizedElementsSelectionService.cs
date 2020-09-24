using System.Collections.Generic;
using Behavioral.Automation.Elements;
using Behavioral.Automation.Services.Mapping;
using JetBrains.Annotations;
using OpenQA.Selenium;

namespace Behavioral.Automation.Services
{
    [UsedImplicitly]
    public sealed class VirtualizedElementsSelectionService : IVirtualizedElementsSelectionService
    {
        private readonly IDriverService _driverService;
        private readonly IAutomationIdProvider _provider;
        private readonly IScopeContextRuntime _contextRuntime;
        private readonly IElementSelectionService _selectionService;

        public VirtualizedElementsSelectionService(
            [NotNull] IDriverService driverService,
            [NotNull] IAutomationIdProvider provider,
            [NotNull] IScopeContextRuntime contextRuntime,
            [NotNull] IElementSelectionService selectionService)
        {
            _driverService = driverService;
            _provider = provider;
            _contextRuntime = contextRuntime;
            _selectionService = selectionService;
        }

        public IEnumerable<T> FindVirtualized<T>(string caption,
            LoadElementsFromCurrentViewCallback<T> loadElementsCallback)
            where T : IWebElementWrapper
        {
            var frameElementToScroll = GetFrameElementToScroll(caption);
            var scrollingArguments = GetScrollingArguments(frameElementToScroll);

            ResetScrollPosition<T>(frameElementToScroll, scrollingArguments.ScrollHeight);

            foreach (var element in loadElementsCallback())
            {
                yield return element;
            }

            for (int i = 1; i <= scrollingArguments.NumberOfScrolls; i++)
            {
                var offset = CalculateScrollingOffset(scrollingArguments, i);
                _driverService.ScrollElementTo(frameElementToScroll, offset);

                foreach (var element in loadElementsCallback())
                {
                    yield return element;
                }
            }
        }

        private static int CalculateScrollingOffset(ScrollingArguments scrollingArguments, int i)
        {
            int offset = scrollingArguments.FrameElementHeight * i;
            if (i == scrollingArguments.NumberOfScrolls)
            {
                offset = scrollingArguments.FrameElementHeight * (i - 1) + scrollingArguments.LastStepOffset;
            }
            return offset;
        }

        public bool IsVirtualizable(string caption)
        {
            _provider.ParseCaption(caption, out var name, out var type);
            var contextForControl = _contextRuntime.FindControlReference(type, name);

            if (contextForControl.ControlLocation != null &&
                contextForControl.ControlLocation.ScopeOptions.IsVirtualized)
            {
                return true;
            }
            return false;
        }

        private static ScrollingArguments GetScrollingArguments(IWebElement frameElementToScroll)
        {
            var scrollHeight = int.Parse(frameElementToScroll.GetAttribute("scrollHeight"));
            var frameElementHeight = frameElementToScroll.Size.Height;
            var numberOfScrolls = (scrollHeight / frameElementHeight) - 1;
            int lastStepOffset = frameElementHeight;
            var remainder = scrollHeight % frameElementHeight;
            if (remainder > 0)
            {
                lastStepOffset = remainder;
                numberOfScrolls++;
            }
            return new ScrollingArguments(numberOfScrolls, lastStepOffset, frameElementHeight, scrollHeight);
        }

        private IWebElement GetFrameElementToScroll(string caption)
        {
            _provider.ParseCaption(caption, out var name, out var type);
            var contextForControl = _contextRuntime.FindControlReference(type, name);
            var frameElementToScrollCaption = contextForControl.ControlLocation.ControlScopeId.Name;
            var frameElementToScroll = _selectionService.Find(frameElementToScrollCaption);
            return frameElementToScroll;
        }

        private void ResetScrollPosition<T>(IWebElement nestedElement, int scrollHeight) where T : IWebElementWrapper
        {
            _driverService.ScrollElementTo(nestedElement, -scrollHeight);
        }

        private sealed class ScrollingArguments
        {
            internal ScrollingArguments(int numberOfScrolls, int lastStepOffset, int frameElementHeight, int scrollHeight)
            {
                NumberOfScrolls = numberOfScrolls;
                LastStepOffset = lastStepOffset;
                FrameElementHeight = frameElementHeight;
                ScrollHeight = scrollHeight;
            }

            public int NumberOfScrolls { get; }
            public int LastStepOffset { get; }
            public int FrameElementHeight { get; }
            public int ScrollHeight { get; }
        }
    }
}
