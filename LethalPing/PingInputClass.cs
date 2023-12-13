using LethalCompanyInputUtils.Api;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;


namespace LethalPing
{
    public class PingInputClass : LcInputActions
    {
        [InputAction("ping", "<Keyboard>/v", "", Name = "Ping")]
        public InputAction pingKey { get; set; }

        public static PingInputClass Instance = new PingInputClass();

    }
}
