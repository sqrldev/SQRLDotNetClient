using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQRLDotNetClientUI.Models
{
    /// <summary>
    /// Calculates a password strength rating.
    /// </summary>
    public class PasswordStrengthMeter
    {
        public const int PW_MIN_LENGTH = 8;
        public const int STRENGTH_POINTS_MIN_MEDIUM = 11;
        public const int STRENGTH_POINTS_MIN_GOOD = 17;

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationToken _ct;
        private Task _workerTask = null;

        public event EventHandler<ScoreUpdatedEventArgs> ScoreUpdated;

        /// <summary>
        /// Creates a PasswordStrengthMeter object.
        /// </summary>
        public PasswordStrengthMeter()
        {
            _ct = _cts.Token;
        }

        /// <summary>
        /// Calculates the strength of the given <paramref name="password"/> and
        /// fires the <c>ScoreUpdated</c> event if the calculation succeeded.
        /// The function protects against too frequent updates by cancelling 
        /// calculations that are still running while a new calculation was already
        /// started. It is therefore safe to call this method in quick succession.
        /// </summary>
        /// <param name="password"></param>
        private void CalculateResult(string password)
        {
            // This will avoid updating the UI too often on quick password input
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(5);
                if (_cts.IsCancellationRequested) return;
            }

            PasswordStrengthResult result = new PasswordStrengthResult();

            result.PasswordLength = password.Length;

            if (result.PasswordLength > PW_MIN_LENGTH)
                result.StrengthPoints = result.PasswordLength;
            else
                result.StrengthPoints = (int)(result.PasswordLength / 2);

            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];

                // Uppercase
                if (c >= 65 && c <= 90) 
                {
                    if (!result.UppercaseUsed)
                    {
                        result.StrengthPoints += 2;
                        result.UppercaseUsed = true;
                    }
                }
                // Lowercase
                else if (c >= 97 && c <= 122) 
                {
                    if (!result.LowercaseUsed)
                    {
                        result.LowercaseUsed = true;
                    }
                }
                // Digit
                else if (c >= 48 && c <= 57)
                {
                    if (!result.DigitsUsed)
                    {
                        result.StrengthPoints += 2;
                        result.DigitsUsed = true;
                    }
                }
                // Symbol
                else
                {
                    if (!result.SymbolsUsed)
                    {
                        result.StrengthPoints += 2;
                        result.SymbolsUsed = true;
                    }
                }

                if (result.AllCharClassesUsed) break; // No need to look any further
                if (_cts.IsCancellationRequested) return;
            }

            if (result.StrengthPoints < STRENGTH_POINTS_MIN_MEDIUM)
            {
                result.Rating = PasswordRating.POOR;
            }
            else if (result.StrengthPoints < STRENGTH_POINTS_MIN_GOOD)
            {
                result.Rating = PasswordRating.MEDIUM;
            }
            else result.Rating = PasswordRating.GOOD;

            // Last chance to detect cancellation
            if (_cts.IsCancellationRequested) return;

            // Fire score updated event
            ScoreUpdated?.Invoke(this, new ScoreUpdatedEventArgs(result));
        }

        /// <summary>
        /// Recalculates the password strength score for <paramref name="password"/> and 
        /// finally fires the <c>ScoreUpdated</c> event.
        /// </summary>
        /// <param name="password">The password to calculate the rating for.</param>
        public void Update(string password)
        {
            if (_workerTask != null)
            {
                if (!_workerTask.IsCompleted)
                    _cts.Cancel();

                // Wait for the prior run to end.
                // This throws if the cancellation already happend, so we need to catch that!
                try { _workerTask.Wait(); }
                catch (Exception) { }
            }

            _cts = new CancellationTokenSource();
            _ct = _cts.Token;

            _workerTask = Task.Run(() => CalculateResult(password), _ct);
        }
    }

    /// <summary>
    /// Represents the results of measuring the strength of a password.
    /// </summary>
    public class PasswordStrengthResult
    {
        public int StrengthPoints = 0;
        public int PasswordLength = 0;

        public bool LowercaseUsed = false;
        public bool UppercaseUsed = false;
        public bool DigitsUsed = false;
        public bool SymbolsUsed = false;

        public PasswordRating Rating = PasswordRating.POOR;

        public bool AllCharClassesUsed
        {
            get { return (LowercaseUsed && UppercaseUsed && DigitsUsed && SymbolsUsed); }
        }
    }

    /// <summary>
    /// Represents a rating for the password strength.
    /// </summary>
    public enum PasswordRating
    {
        POOR,
        MEDIUM,
        GOOD
    }

    /// <summary>
    /// Represents event arguments for the <c>ScoreUpdated</c> event.
    /// </summary>
    public class ScoreUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// The result of the password strength calculation.
        /// </summary>
        public PasswordStrengthResult Score;

        public ScoreUpdatedEventArgs(PasswordStrengthResult passwordScore)
        {
            Score = passwordScore;
        }
    }
}