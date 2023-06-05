using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace BuildTools
{
    class ValidateSetExAttribute : ValidateEnumeratedArgumentsAttribute
    {
        private static ConstructorInfo exCtor;

        private readonly IValidateSetValuesGenerator validValuesGenerator;

        // The valid values generator cache works across 'ValidateSetAttribute' instances.
        private static readonly ConcurrentDictionary<Type, IValidateSetValuesGenerator> s_ValidValuesGeneratorCache = new ConcurrentDictionary<Type, IValidateSetValuesGenerator>();

        static ValidateSetExAttribute()
        {
            exCtor = typeof(ValidationMetadataException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(c => c.GetParameters().Length == 4);
        }

        /// <summary>
        /// Gets or sets the custom error message that is displayed to the user.
        /// The item being validated and a text representation of the validation set is passed as the
        /// first and second formatting argument to the <see cref="ErrorMessage"/> formatting pattern.
        /// <example>
        /// <code>
        /// [ValidateSet("A","B","C", ErrorMessage="The item '{0}' is not part of the set '{1}'.")
        /// </code>
        /// </example>
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets a flag specifying if we should ignore the case when performing string comparison.
        /// The default is true.
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        public ValidateSetExAttribute(Type valuesGeneratorType)
        {
            if (!typeof(IValidateSetValuesGenerator).IsAssignableFrom(valuesGeneratorType))
            {
                throw new ArgumentException(nameof(valuesGeneratorType));
            }

            //If we're a generic type definition, we're likely defined on a base cmdlet type. The parameter containing the ValidateSetExAttribute
            //will be overridden
            if (valuesGeneratorType.IsGenericTypeDefinition)
                return;

            // Add a valid values generator to the cache.
            // We don't cache valid values; we expect that valid values will be cached in the generator.
            validValuesGenerator = s_ValidValuesGeneratorCache.GetOrAdd(valuesGeneratorType, (key) => (IValidateSetValuesGenerator)Activator.CreateInstance(key));
        }

        public IList<string> ValidValues
        {
            get
            {
                var validValuesLocal = validValuesGenerator.GetValidValues();

                if (validValuesLocal == null)
                {
                    throw (ValidationMetadataException) exCtor.Invoke(
                        new object[]
                        {
                            "ValidateSetGeneratedValidValuesListIsNull",
                            null,
                            "Valid values generator return a null value."
                        }
                    );
                }

                return validValuesLocal;
            }
        }

        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw (ValidationMetadataException)exCtor.Invoke(
                    new object[]
                    {
                        "ArgumentIsEmpty",
                        null,
                        "The argument is null. Provide a valid value for the argument, and then try running the command again."
                    }
                );
            }

            string objString = element.ToString();

            foreach (string setString in ValidValues)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(
                        setString,
                        objString,
                        IgnoreCase ? CompareOptions.IgnoreCase : CompareOptions.None) == 0)
                {
                    return;
                }
            }

            var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage) ? "The argument \"{0}\" does not belong to the set \"{1}\" specified by the ValidateSet attribute. Supply an argument that is in the set and then try the command again." : ErrorMessage;

            throw (ValidationMetadataException)exCtor.Invoke(
                new object[]
                {
                    "ValidateSetFailure",
                    null,
                    errorMessageFormat,
                    element.ToString(), SetAsString()
                }
            );
        }

        private string SetAsString() => string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, ValidValues);
    }
}