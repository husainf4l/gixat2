using System.Diagnostics.CodeAnalysis;

namespace GixatBackend.Modules.Appointments.Enums;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Required to be public for HotChocolate type discovery")]
public enum AppointmentType
{
    GeneralService = 0,
    OilChange = 1,
    BrakeService = 2,
    TireChange = 3,
    Inspection = 4,
    Diagnosis = 5,
    Repair = 6,
    Consultation = 7,
    AirConditioningService = 8,
    BatteryReplacement = 9,
    EngineRepair = 10,
    TransmissionService = 11,
    Other = 99
}
