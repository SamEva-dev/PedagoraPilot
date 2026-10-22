namespace PedagoraPilot.Domain.Signatures;

public enum SignatureSourceType { PedagogicalObservation = 1, Attendance = 2, DailyObservationSummary = 3 }
public enum SignatureEnvelopeStatus { Draft = 1, Pending = 2, Viewed = 3, Signed = 4, Declined = 5, Expired = 6, Cancelled = 7 }
