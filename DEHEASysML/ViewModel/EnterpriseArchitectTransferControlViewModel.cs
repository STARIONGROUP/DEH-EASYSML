// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnterpriseArchitectTransferControlViewModel.cs" company="RHEA System S.A.">
// Copyright (c) 2020-2022 RHEA System S.A.
// 
// Author: Sam Gerené, Alex Vorobiev, Alexander van Delft, Nathanael Smiechowski, Antoine Théate.
// 
// This file is part of DEHEASysML
// 
// The DEHEASysML is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
// 
// The DEHEASysML is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with this program; if not, write to the Free Software Foundation,
// Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace DEHEASysML.ViewModel
{
    using System.Threading.Tasks;

    using DEHEASysML.DstController;

    using DEHPCommon.Enumerators;
    using DEHPCommon.Services.ExchangeHistory;
    using DEHPCommon.UserInterfaces.ViewModels;
    using DEHPCommon.UserInterfaces.ViewModels.Interfaces;

    using NLog;

    using ReactiveUI;

    using System;
    using System.Diagnostics;
    using System.Reactive.Linq;

    /// <summary>
    ///     <inheritdoc cref="TransferControlViewModel" />
    /// </summary>
    public class EnterpriseArchitectTransferControlViewModel : TransferControlViewModel
    {
        /// <summary>
        /// The <see cref="IDstController" />
        /// </summary>
        private readonly IDstController dstController;

        /// <summary>
        /// The <see cref="IExchangeHistoryService" />
        /// </summary>
        private readonly IExchangeHistoryService exchangeHistoryService;

        /// <summary>
        /// The <see cref="Logger" />
        /// </summary>
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The <see cref="IStatusBarControlViewModel" />
        /// </summary>
        private readonly IStatusBarControlViewModel statusBar;

        /// <summary>
        /// Backing field for <see cref="CanTransfer" />
        /// </summary>
        private bool canTransfer;

        /// <summary>
        /// Backing field for <see cref="TransferInProgress" />
        /// </summary>
        private bool transferInProgress;

        /// <summary>
        /// Initializes a new <see cref="EnterpriseArchitectTransferControlViewModel" />
        /// </summary>
        /// <param name="dstController">The <see cref="IDstController" /></param>
        /// <param name="statusBar">The <see cref="IStatusBarControlViewModel" /></param>
        /// <param name="exchangeHistory">The <see cref="IExchangeHistoryService" /></param>
        public EnterpriseArchitectTransferControlViewModel(IDstController dstController, IStatusBarControlViewModel statusBar,
            IExchangeHistoryService exchangeHistory)
        {
            this.dstController = dstController;
            this.statusBar = statusBar;
            this.exchangeHistoryService = exchangeHistory;

            this.InitializesCommandsAndObservables();
        }

        /// <summary>
        /// Asserts that the <see cref="TransferControlViewModel.TransferCommand" /> can be executed
        /// </summary>
        public bool CanTransfer
        {
            get => this.canTransfer;
            set => this.RaiseAndSetIfChanged(ref this.canTransfer, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="TransferControlViewModel.TransferCommand" /> is executing
        /// </summary>
        public bool TransferInProgress
        {
            get => this.transferInProgress;
            set => this.RaiseAndSetIfChanged(ref this.transferInProgress, value);
        }

        /// <summary>
        /// Update the <see cref="TransferControlViewModel.NumberOfThing" />
        /// </summary>
        public void UpdateNumberOfThingsToTransfer()
        {
            this.NumberOfThing = this.dstController.MappingDirection == MappingDirection.FromDstToHub
                ? this.dstController.SelectedDstMapResultForTransfer.Count
                : 0;

            this.CanTransfer = this.NumberOfThing > 0;
        }

        /// <summary>
        /// Cancels the transfer in progress
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        private async Task CancelTransfer()
        {
            this.dstController.DstMapResult.Clear();
            this.dstController.SelectedDstMapResultForTransfer.Clear();
            this.exchangeHistoryService.ClearPending();
            await Task.Delay(1);
            this.TransferInProgress = false;
            this.IsIndeterminate = false;
        }

        /// <summary>
        /// Initializes all <see cref="ReactiveCommand{T}" /> and <see cref="Observable" /> of this view model
        /// </summary>
        private void InitializesCommandsAndObservables()
        {
            this.dstController.SelectedDstMapResultForTransfer.CountChanged.Subscribe(_ => this.UpdateNumberOfThingsToTransfer());

            this.WhenAnyValue(x => x.dstController.MappingDirection)
                .Subscribe(_ => this.UpdateNumberOfThingsToTransfer());

            this.TransferCommand = ReactiveCommand.CreateAsyncTask(
                this.WhenAnyValue(x => x.CanTransfer),
                async _ => await this.TransferCommandExecute(),
                RxApp.MainThreadScheduler);

            this.TransferCommand.ThrownExceptions
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(exception =>
                {
                    this.statusBar.Append($"{exception.Message}", StatusBarMessageSeverity.Error);
                    this.logger.Error(exception);
                });

            this.CancelCommand = ReactiveCommand.CreateAsyncTask(
                this.WhenAnyValue(x => x.TransferInProgress), async _ => await this.CancelTransfer(),
                RxApp.MainThreadScheduler);
        }

        /// <summary>
        /// Executes the <see cref="TransferControlViewModel.TransferCommand" />
        /// </summary>
        /// <returns>A <see cref="Task" /></returns>
        private async Task TransferCommandExecute()
        {
            var timer = new Stopwatch();
            timer.Start();
            this.TransferInProgress = true;
            this.IsIndeterminate = true;
            this.statusBar.Append("Transfer in progress");

            if (this.dstController.MappingDirection is MappingDirection.FromDstToHub)
            {
                await this.dstController.TransferMappedThingsToHub();
            }

            await this.exchangeHistoryService.Write();
            timer.Stop();
            this.statusBar.Append($"Transfers completed in {timer.ElapsedMilliseconds} ms");
            this.IsIndeterminate = false;
            this.TransferInProgress = false;
        }
    }
}
