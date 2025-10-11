namespace AcademicAssessment.Core.Enums;

/// <summary>
/// Subscription tiers for self-service (B2C) students
/// </summary>
public enum SubscriptionTier
{
    /// <summary>
    /// Free tier - limited assessments and features
    /// </summary>
    Free = 0,

    /// <summary>
    /// Basic tier - $9.99/month - unlimited practice, basic analytics
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Premium tier - $19.99/month - all features, personalized learning
    /// </summary>
    Premium = 2,

    /// <summary>
    /// School-based subscription (B2B) - pricing negotiated per school
    /// </summary>
    School = 3
}
