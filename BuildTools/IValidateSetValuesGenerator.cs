namespace BuildTools
{
    //Redeclared here as this interface is only present in PowerShell 6+
    interface IValidateSetValuesGenerator
    {
        string[] GetValidValues();
    }
}