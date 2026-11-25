namespace AutoDocs.Core.Model;

public enum DiscrepancyKind
{
    EndpointAdded,
    EndpointRemoved,

    ParameterAdded,
    ParameterRemoved,
    ParameterTypeChanged,
    ParameterSourceChanged,
    ParameterRequiredChanged,
    ParameterDescriptionChanged,

    ResponseAdded,
    ResponseRemoved,
    ResponseTypeChanged,

    SummaryChanged,
    ReturnsChanged
}