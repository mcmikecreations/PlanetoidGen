namespace PlanetoidGen.Contracts.Constants.StringMessages
{
    public static class GeneralStringMessages
    {
        private const string Prefix = "General";

        public static readonly string ObjectNotInitialized = $"{Prefix}_{nameof(ObjectNotInitialized)}";
        public static readonly string ObjectNotExist = $"{Prefix}_{nameof(ObjectNotExist)}";
        public static readonly string OperationNotSupported = $"{Prefix}_{nameof(OperationNotSupported)}";
        public static readonly string InternalError = $"{Prefix}_{nameof(InternalError)}";

        public static readonly string DatabaseProcedureError = $"{Prefix}_{nameof(DatabaseProcedureError)}";
        public static readonly string DatabaseProcedureRecordNotExist = $"{Prefix}_{nameof(DatabaseProcedureRecordNotExist)}";
    }
}
