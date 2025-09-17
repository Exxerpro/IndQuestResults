using System;
using System.Collections.Generic;
using System.Linq;

namespace IndQuestResults.Simple
{
    /// <summary>
    /// Simple test class to demonstrate Stryker mutation testing capabilities
    /// This class contains deliberately simple logic that Stryker can mutate effectively
    /// </summary>
    public class SimpleResultValidator
    {
        /// <summary>
        /// Validates if a collection of error messages is valid for Result creation
        /// </summary>
        public static bool IsValidErrorCollection(IEnumerable<string> errors)
        {
            if (errors == null)
                return false;
            
            var errorList = errors.ToList();
            
            // Must have at least one error
            if (errorList.Count == 0)
                return false;
            
            // All errors must be non-null and non-empty
            return errorList.All(error => !string.IsNullOrWhiteSpace(error));
        }
        
        /// <summary>
        /// Calculates a simple risk score based on error count
        /// </summary>
        public static int CalculateRiskScore(IEnumerable<string> errors)
        {
            if (errors == null)
                return 0;
            
            var errorCount = errors.Count();
            
            if (errorCount <= 1)
                return 1; // Low risk
            
            if (errorCount <= 3)
                return 2; // Medium risk
            
            return 3; // High risk
        }
        
        /// <summary>
        /// Determines if a result should be retried based on error patterns
        /// </summary>
        public static bool ShouldRetry(IEnumerable<string> errors, int currentAttempt)
        {
            if (errors == null || currentAttempt >= 3)
                return false;
            
            var errorList = errors.ToList();
            
            // Don't retry if there are too many different errors
            if (errorList.Count > 5)
                return false;
            
            // Retry if any error contains "timeout" or "connection"
            return errorList.Any(error => 
                error.ToLowerInvariant().Contains("timeout") || 
                error.ToLowerInvariant().Contains("connection"));
        }
    }
    
    /// <summary>
    /// Simple test runner that can verify our validator works
    /// </summary>
    public class SimpleTestRunner
    {
        public static void RunBasicTests()
        {
            // Test 1: Valid error collection
            var validErrors = new[] { "Error 1", "Error 2" };
            var isValid = SimpleResultValidator.IsValidErrorCollection(validErrors);
            if (!isValid)
                throw new Exception("Test 1 failed: Valid errors should be valid");
            
            // Test 2: Empty collection should be invalid
            var emptyErrors = new string[0];
            var isEmpty = SimpleResultValidator.IsValidErrorCollection(emptyErrors);
            if (isEmpty)
                throw new Exception("Test 2 failed: Empty errors should be invalid");
            
            // Test 3: Null collection should be invalid
            var isNull = SimpleResultValidator.IsValidErrorCollection(null);
            if (isNull)
                throw new Exception("Test 3 failed: Null errors should be invalid");
            
            // Test 4: Risk score calculation
            var lowRisk = SimpleResultValidator.CalculateRiskScore(new[] { "One error" });
            if (lowRisk != 1)
                throw new Exception("Test 4 failed: Single error should be low risk");
            
            var mediumRisk = SimpleResultValidator.CalculateRiskScore(new[] { "Error 1", "Error 2" });
            if (mediumRisk != 2)
                throw new Exception("Test 5 failed: Two errors should be medium risk");
            
            var highRisk = SimpleResultValidator.CalculateRiskScore(new[] { "E1", "E2", "E3", "E4" });
            if (highRisk != 3)
                throw new Exception("Test 6 failed: Four errors should be high risk");
            
            // Test 7: Retry logic
            var timeoutErrors = new[] { "Connection timeout occurred" };
            var shouldRetry = SimpleResultValidator.ShouldRetry(timeoutErrors, 1);
            if (!shouldRetry)
                throw new Exception("Test 7 failed: Timeout errors should trigger retry");
            
            var tooManyAttempts = SimpleResultValidator.ShouldRetry(timeoutErrors, 3);
            if (tooManyAttempts)
                throw new Exception("Test 8 failed: Too many attempts should not retry");
        }
    }
}