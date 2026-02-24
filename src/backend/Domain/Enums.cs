namespace EdgeFront.Builder.Domain;

public enum SeriesStatus { Draft, Published }
public enum SessionStatus { Draft, Published }
public enum ReconcileStatus { Synced, Reconciling, Retrying, Disabled }
public enum DriftStatus { None, DriftDetected }
public enum WarmRule { W1, W2 }
public enum ChangeType { Registration, AttendanceReport }
