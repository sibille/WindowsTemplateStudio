﻿//{**
// These code blocks include the LiveTileFeatureService Instance in the method `GetActivationHandlers()`,
// enable the notification queue in the method `InitializeAsync()` and add a sample LiveTile to the method 
// `StartupAsync()` in the ActivationService of your project.
//**}

using System;
//{[{
using Param_RootNamespace.Helpers;
//}]}

namespace Param_ItemNamespace.Services
{
    internal class ActivationService
    {
        private async Task InitializeAsync()
        {
            //{[{
            await Singleton<LiveTileFeatureService>.Instance.EnableQueueAsync();
            //}]}
        }

        private async Task StartupAsync()
        {
            //^^
            //{[{
            Singleton<LiveTileFeatureService>.Instance.SampleUpdate();
            //}]}
            await Task.CompletedTask;
        }

        private IEnumerable<ActivationHandler> GetActivationHandlers()
        {
//{[{
            yield return Singleton<LiveTileFeatureService>.Instance;
//}]}
//{--{

            yield break;//}--}
        }
    }
}
