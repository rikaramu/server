using System;
using System.Collections.Generic;
using System.Linq;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Test.AutoFixture;
using Bit.Core.Test.AutoFixture.SendFixtures;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Services
{
    public class SendServiceTests
    {
        private void SaveSendAsync_Setup(SendType sendType, bool canManagePolicies,
            SutProvider<SendService> sutProvider, Send send, List<Policy> policies)
        {
            send.Id = default;
            send.Type = sendType;

            policies.First().Type = PolicyType.DisableSend;
            policies.First().Enabled = true;

            sutProvider.GetDependency<IPolicyRepository>().GetManyByUserIdAsync(send.UserId.Value).Returns(policies);
            sutProvider.GetDependency<ICurrentContext>().ManagePolicies(Arg.Any<Guid>()).Returns(canManagePolicies);
        }

        [Theory]
        [InlineUserSendAutoData(SendType.File)]
        [InlineUserSendAutoData(SendType.Text)]
        public async void SaveSendAsync_DisableSend_CantManagePolicies_throws(SendType sendType,
            SutProvider<SendService> sutProvider, Send send, List<Policy> policies)
        {
            SaveSendAsync_Setup(sendType, canManagePolicies: false, sutProvider, send, policies);

            await Assert.ThrowsAsync<BadRequestException>(() => sutProvider.Sut.SaveSendAsync(send));
        }

        [Theory]
        [InlineUserSendAutoData(SendType.File)]
        [InlineUserSendAutoData(SendType.Text)]
        public async void SaveSendAsync_DisableSend_DisabledPolicy_CantManagePolicies_success(SendType sendType,
            SutProvider<SendService> sutProvider, Send send, List<Policy> policies)
        {
            SaveSendAsync_Setup(sendType, canManagePolicies: false, sutProvider, send, policies);
            foreach (var policy in policies.Where(p => p.Type == PolicyType.DisableSend))
            {
                policy.Enabled = false;
            }

            await sutProvider.Sut.SaveSendAsync(send);

            await sutProvider.GetDependency<ISendRepository>().Received(1).CreateAsync(send);
        }

        [Theory]
        [InlineUserSendAutoData(SendType.File)]
        [InlineUserSendAutoData(SendType.Text)]
        public async void SaveSendAsync_DisableSend_CanManagePolicies_success(SendType sendType,
            SutProvider<SendService> sutProvider, Send send, List<Policy> policies)
        {
            SaveSendAsync_Setup(sendType, canManagePolicies: true, sutProvider, send, policies);

            await sutProvider.Sut.SaveSendAsync(send);

            await sutProvider.GetDependency<ISendRepository>().Received(1).CreateAsync(send);
        }
    }
}
