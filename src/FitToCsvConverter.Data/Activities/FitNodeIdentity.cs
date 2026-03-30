namespace FitToCsvConverter.Data.Activities;

public readonly record struct FitNodeIdentity(FitNodeType NodeType, int SequenceNumber, ushort? MessageIndex);
