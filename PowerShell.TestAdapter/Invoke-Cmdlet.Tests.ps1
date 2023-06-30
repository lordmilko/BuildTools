# Test file for F5 debugging PowerShell.TestAdapter
# Note that when we debug netstandard2.0 it thinks vstest.console is a .NET Core application,
# so we never attach to it. Temporarily changing the target framework to net472 lets us debug

Describe "Invoke-Cmdlet" {
    Context "My Context" {
        It "completes successfully" {
            $i = 0
        }

        It "throws" {
            throw
        }
    }
}