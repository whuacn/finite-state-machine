﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bitcraft.StateMachine.UnitTests.AutoGenerated
{
    partial class TestAutoEndState
    {
        protected override void OnEnter(StateEnterEventArgs e)
        {
            base.OnEnter(e);

            var context = (StateMachineTestContext)Context;

            Assert.AreEqual(context.TestStatus, 4);
            context.TestStatus++;
        }

        private void OnTestAutoFinalizeAction(object data, Action<StateToken> callback)
        {
            callback(null);
        }

        protected override void OnExit()
        {
            base.OnExit();

            var context = (StateMachineTestContext)Context;

            Assert.AreEqual(context.TestStatus, 5);
            context.TestStatus++;
        }
    }
}
