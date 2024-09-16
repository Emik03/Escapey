// SPDX-License-Identifier: MPL-2.0
namespace Escapey.ML;

/// <summary>Represents the prediction of a phoneme from the model.</summary>
sealed class Prediction
{
    /// <summary>Gets the predicted phoneme.</summary>
    [NoColumn]
    public Sprite.Mouth Mouth => (Sprite.Mouth)(PredictedLabel + (float)Sprite.Mouth.Upset);

    /// <summary>Gets or sets the predicted label.</summary>
    public float PredictedLabel { get; set; }
}
