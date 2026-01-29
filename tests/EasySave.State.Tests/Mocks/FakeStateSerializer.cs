using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.State;

public class FakeStateSerializer : StateSerializer
{
    public bool WasCalled { get; private set; }

    public override void WritePrettyJson(string path, GlobalState state)
    {
        WasCalled = true;
    }
}
