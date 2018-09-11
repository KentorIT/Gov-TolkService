// Write your JavaScript code.

// Set up date-picker. See docs at https://bootstrap-datepicker.readthedocs.io/en/latest/markup.html
var datePickerOptions = {
    language: 'sv',
    calendarWeeks: true,
    todayHighlight: true,
    clearBtn: true,
    format: {
        toDisplay: function (date, format, language) {
            return date.toISOString().slice(0, 10);
        },
        toValue: function (date, format, language) {
            return new Date(formatDate(date));
        }
    }
};

// Auto-format date entered with just digits.
function formatDate(date) {
    if (/^[0-9]{6}$/.test(date)) {
        date = "20" + date;
    }
    if (/^[0-9]{8}$/.test(date)) {
        date = date.substring(0, 4) + "-" + date.substring(4, 6) + "-" + date.substring(6);
        return date;
    }
}

$('.datepicker').datepicker(datePickerOptions);

$('.date .input-group-addon').click(function () {
    $(this).prev().datepicker('show');
});

$('.input-daterange input').datepicker(datePickerOptions);

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
    if (Globalize && $.validator) {
        $.validator.methods.number = function (value, element) {
            return value === "" || value === "0" || Globalize.parseFloat(value);
        };
        Globalize.culture('sv-SE');
    }
    $("form:not(.do-not-check-dirty)").areYouSure();

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
    }
});
