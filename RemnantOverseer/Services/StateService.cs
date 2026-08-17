using System;
using System.Collections.Generic;
using System.Text;

namespace RemnantOverseer.Services;

// Turns out that having a place to save (some part of) the current state might be desirable. Who knew?
// Very simple implementation since I do not expect to store a lot here. Envisioned mostly as a place
// to store data that might be changed before all views had a chance to set up message handlers.
public class StateService
{
    public StateService() { }

    public int? SelectedCharacterIndex { get; set; }
}
