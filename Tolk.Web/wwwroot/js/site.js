// Write your JavaScript code.

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
$('.datepicker').datepicker({
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true
});

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
});

$('#impersonation-select').change(function () {
    $("#impersonation-form").submit();
});

$(function () {
    $("body").on("click", "table.clickable-rows > tbody > tr > td", function () {
        var $table = $(this).parents("table.clickable-rows");
        var $parameterName = $table.data("click-parameter");
        var $parameter = $(this).parent("tr").data($parameterName);
        window.location.href = tolkBaseUrl + $table.data("click-controller") + "/" + $table.data("click-action") + "?" + $parameterName + "=" + $parameter;
    });
 
});
$.fn.extend({
    bindEnterKey: function (input, button, context) {
        var $context = context ? $(context) : $(this);
        $(this).find(input).off('keypress');
        $(this).find(input).on('keypress', function (e) {
            if ((e.keyCode || e.which) === 13) {
                $context.find(button).click();
                e.preventDefault();
            }
        });
    },
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
