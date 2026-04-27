namespace BionicAthlete.Training.Domain.Activities;

public readonly record struct FitNodeIdentity(FitNodeType NodeType, int SequenceNumber, ushort? MessageIndex);
