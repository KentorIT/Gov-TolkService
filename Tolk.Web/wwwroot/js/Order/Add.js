// Write your JavaScript code.
$(function () {
    $("body").on("change", "#SpecialRequirements", function () {
        $(this).toggleOccasionalField($("#SpecialRequirementsText-wrapper"), true);
    });
    $("body").on("change", "#SpecialNeeds", function () {
        $(this).toggleOccasionalField($("#SpecialNeeds-wrapper"), true);
    });
});
$.fn.extend({
    toggleOccasionalField: function (occasionalField, visibleOnValue) {
        var isSelected = false;
        if (Array.isArray(visibleOnValue)) {
            for (var i = 0; i < visibleOnValue.length; ++i) {
                if ($(this).hasValue(visibleOnValue[i])) {
                    isSelected = true;
                    break;
                }
            }
        } else {
            isSelected = $(this).hasValue(visibleOnValue);
        }

        if (isSelected) {
            occasionalField.show(200);
        } else {
            occasionalField.hide(200);
        }
    },
    hasValue: function (value) {
        //Intentional use of == iso === since "1" and 1 needs to be seen as equal..
        return ($(this).is(":checkbox")) ? $(this).is(":checked") === value : ($(this).val() == value);
    }
});
