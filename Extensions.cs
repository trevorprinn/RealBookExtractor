using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace RealBookExtractor {
    static class Extensions {

        public static void BeginInvokeIfRequired(this ISynchronizeInvoke obj, MethodInvoker action) {
            if (obj.InvokeRequired) {
                var args = new object[0];
                obj.BeginInvoke(action, args);
            } else {
                action();
            }
        }
    }
}
