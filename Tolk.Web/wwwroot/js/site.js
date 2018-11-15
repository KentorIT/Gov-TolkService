﻿// Write your JavaScript code.

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
var datePickerOptions = {
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true,
    clearBtn: true,
    orientation: "bottom",
    format: {
        toDisplay: function (date, format, language) {
            return date.toISOString().slice(0, 10);
        },
        toValue: function (date, format, language) {
            return new Date(Date.customFormat(date));
        }
    }
};

var orderDatePickerOptions = jQuery.extend({}, datePickerOptions);
orderDatePickerOptions.startDate = new Date(Number($("#SystemTime").val()))
    .addDays(1)
    .zeroTime();

$('.datepicker').not('.order-datepicker .datepicker').datepicker(datePickerOptions);
$('.order-datepicker .datepicker').datepicker(orderDatePickerOptions);

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
});

$('.input-daterange input').not('.order-datepicker .input-daterange input').datepicker(datePickerOptions);
$('.order-datepicker .input-daterange input').datepicker(orderDatePickerOptions);

$('.input-daterange input').click(function () {
    $(this).datepicker('show');
});

$('#impersonation-select').change(function () {
    $("#impersonation-form").submit();
});

$('#timeTravelDatePicker').on('changeDate', function () {
    $('#timeTravelDate').val(
        $('#timeTravelDatePicker').datepicker('getFormattedDate')
    );
});

function updateTime() {
    var date = new Date(new Date().getTime() + Number($('#now').attr('data-timetravel-milliseconds')));
    $('#now').text(date.toLocaleString("sv-SE"));
}

if ($('#now').length === 1) {
    updateTime();
    setInterval(updateTime, 1000);
}

$(function () {
    var dirty = "dirty";

    if (Globalize && $.validator) {
        $.validator.methods.number = function (value, element) {
            return value === "" || value === "0" || Globalize.parseFloat(value);
        };
        Globalize.culture('sv-SE');
    }
    $("form:not(.do-not-check-dirty)").areYouSure({
        dirtyClass: dirty,
        message: "Du har osparade ändringar!"
    });

    $("div.autofocus > input").first().focus();

    // Whitelist all inputs and textareas for Hotjar recordings
    $("input").addClass("data-hj-whitelist");
    $("textarea").addClass("data-hj-whitelist");

    // For buttons and anchors that ignore dirty-checks
    $("form > :button.do-not-check-dirty, form > a.do-not-check-dirty").on("click", function () {
        $(this).parent("form").removeClass(dirty);
    });

    $("form.filter-form").on("change", "select, input, textarea", function (event) {
        $(this).closest("form").submit();
    });

    $(".table-datatable table").DataTable({
        searching: false,
        language: {
            url: "//cdn.datatables.net/plug-ins/1.10.19/i18n/Swedish.json"
        }
    });

    $("select").each(function () {
        var allowClear = $(this).parent().hasClass("allow-clear");
        $(this).select2({ minimumResultsForSearch: 10, allowClear: allowClear });
    });
  
    $("body").on("click", "table.clickable-rows-with-action > tbody > tr > td", function () {
        var $row = $(this).closest("tr");
        window.location.href = $row.data("click-action-url");
    });
 
});

$('.more-info-price').click(function () {
    $(this).closest('.wrap-price-info').find('.detail-price-info').collapse('toggle');
    if ($(this).text().indexOf("Visa") !== -1) {
        $(this).html($(this).html().replace("Visa", "Dölj"));
    }
    else {
        $(this).html($(this).html().replace("Dölj", "Visa"));
    }
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
        return $(this).is(":checkbox")
            ? $(this).is(":checked") === value
            : $(this).val() == value; //eslint-disable-line eqeqeq
    },

    // wizard
    tolkWizard: function (opts) {
        var $wizard = $(".wizard");
        var options = $.extend({}, opts);
        opts.validationFalseHandler = function (result) { result.focusInvalid(); }
        opts.onloadHandler = function () {
            $wizard.find(".wizard-step:not(.wizard-step-hidden)").each(function () {
                if (options.onloadHandler !== undefined) {
                    options.onloadHandler();
                }
            });
        };
        $wizard.wizardFormValidation(opts);
    }
});
