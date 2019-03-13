namespace RangerFS.Tests

open Expecto

module RunTests =

    [<EntryPoint>]
    let main args =
        Tests.runTestsInAssembly defaultConfig args
