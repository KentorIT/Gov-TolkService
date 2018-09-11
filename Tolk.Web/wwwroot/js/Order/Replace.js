﻿
$(function () {
    $.validator.addMethod("staywithin", function (value, element, options) {
        var $fields = $(options, element.form),
            $fieldsFirst = $fields.eq(0),
            validator = $fieldsFirst.data("valid_req_grp") ? $fieldsFirst.data("valid_req_grp") : $.extend({}, this);
        if (element.id !== $fieldsFirst[0].id) {
            isValid = true;
        } else {
            var thisPrefix = element.id.split('_')[0];
            isValid = test(thisPrefix, $(element).data("rule-otherproperty"));
        }
        // Store the cloned validator for future validation
        $fieldsFirst.data("valid_req_grp", validator);

        // If element isn't being validated, run each require_from_group field's validation rules
        if (!$(element).data("being_validated")) {
            $fields.data("being_validated", true);
            $fields.each(function () {
                validator.element(this);
            });
            $fields.data("being_validated", false);
        }
        return isValid;
    }, $.validator.format("Ooops."));

    var test = function (thisPrefix, otherPrefix) {
        var dateField = new Date($("#" + otherPrefix +  "_StartDate").val()).toLocaleDateString("sv-SE");
        var startField = $("#" + otherPrefix +  "_StartTime").val();
        var endField = $("#" + otherPrefix +  "_EndTime").val();
        var isTwoDay = startField > endField;
        var nextDay = new Date($("#" + otherPrefix +  "_StartDate").val());
        nextDay.setDate(nextDay.getDate() + 1);
        //Find the TimeRange_ part of the current input's name, to find the other inputs
        var dateFieldChanged = new Date($("#" + thisPrefix +  "_StartDate").val()).toLocaleDateString("sv-SE");
        var startFieldChanged = $("#" + thisPrefix +  "_StartTime").val();
        var endFieldChanged = $("#" + thisPrefix +  "_EndTime").val();
        if ((!isTwoDay && dateField !== dateFieldChanged) || (isTwoDay && dateField !== dateFieldChanged && dateFieldChanged !== nextDay)) {
            return false;
        } else {
            if (isTwoDay) {
                if (dateField === dateFieldChanged) {
                    return startFieldChanged >= startField &&
                        (startFieldChanged < endFieldChanged || endFieldChanged <= endField);
                } else {
                    return startFieldChanged < endField &&
                        startFieldChanged < endFieldChanged &&
                        endFieldChanged <= endField;
                }
            } else {
                return startFieldChanged >= startField &&
                    startFieldChanged < endFieldChanged &&
                    endFieldChanged <= endField;
            }
        }
    };

    $('form').validate({
        //onfocusout: true,
        groups: {
            timerange: "TimeRange.StartTime TimeRange_StartTime TimeRange.EndTime"
        },
        errorPlacement: function (error, element) {
            if (element.attr("name") === "TimeRange.StartTime" || element.attr("name") === "TimeRange_StartTime" || element.attr("name") === "TimeRange.EndTime") {
                error.insertAfter("#TimeRange_EndTime");
            } else {
                error.insertAfter(element);
            }
        }
    });
});
