$(function () {
    function sanitizeTimeInput(value) {
        if (value.indexOf(":") === -1) {
            var colonPos = value.length - 2;
            value = [value.slice(0, colonPos), ":", value.slice(colonPos)].join("");
        }
        if (value.length === 4) {
            value = ["0", value].join("");
        }
        return value;
    }
    $.validator.addMethod("staywithin", function (value, element, options) {
        var thisPrefix = element.id.split('_')[0];
        return test(thisPrefix, $(element).data("rule-otherproperty"), $("#" + $(element).data("rule-rulesetproperty")).val());
    }, $.validator.format("Not within the provided time range"));

    var test = function (thisPrefix, otherPrefix, ruleset) {
        var dateField = new Date($("#" + otherPrefix + "_StartDate").val()).toLocaleDateString("sv-SE");
        var startField = $("#" + otherPrefix + "_StartTime").val();
        var endField = $("#" + otherPrefix + "_EndTime").val();
        var isTwoDay = startField > endField;
        var nextDay = new Date($("#" + otherPrefix + "_StartDate").val());
        nextDay.setDate(nextDay.getDate() + 1);
        var nextDayField = nextDay.toLocaleDateString("sv-SE");
        //Find the TimeRange_ part of the current input's name, to find the other inputs
        var dateFieldChanged = new Date($("#" + thisPrefix + "_StartDate").val()).toLocaleDateString("sv-SE");
        var startFieldChanged = sanitizeTimeInput($("#" + thisPrefix + "_StartTime").val());
        var endFieldChanged = sanitizeTimeInput($("#" + thisPrefix + "_EndTime").val());
        if (ruleset === "VersionOne") {
            //Rule: The new time needs to be completely inside the bounds of the original
            if ((!isTwoDay && dateField !== dateFieldChanged) || (isTwoDay && dateField !== dateFieldChanged && dateFieldChanged !== nextDayField)) {
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
        } else if (ruleset === "VersionTwo") {
            //Rule: The new time needs to: 1. overlap at least five minutes at start or end of the original, and 2. be as long or shorter than the original.
            var startDate = new Date($("#" + otherPrefix + "_StartDate").val());
            var originalStartDateTime = new Date(startDate);
            originalStartDateTime.setHours(parseInt(startField.split(":")[0]), parseInt(startField.split(":")[1]));

            var originalEndDateTime = new Date(startDate);
            originalEndDateTime.setDate(startDate.getDate() + (isTwoDay ? 1 : 0));
            originalEndDateTime.setHours(parseInt(endField.split(":")[0]), parseInt(endField.split(":")[1]));
            var changedDate = new Date($("#" + thisPrefix + "_StartDate").val());
            var changedStartDateTime = new Date(changedDate);
            changedStartDateTime.setHours(parseInt(startFieldChanged.split(":")[0]), parseInt(startFieldChanged.split(":")[1]));
            var changedEndDateTime = new Date(changedDate);
            if (startFieldChanged > endFieldChanged) {
                changedEndDateTime.setDate(changedStartDateTime.getDate() + 1);
            }
            changedEndDateTime.setHours(parseInt(endFieldChanged.split(":")[0]), parseInt(endFieldChanged.split(":")[1]));
            //Check duration
            if ((originalEndDateTime.getTime() - originalStartDateTime.getTime()) < (changedEndDateTime.getTime() - changedStartDateTime.getTime())) {
                return false;
            }
            //Check overlap at start
            if ((originalStartDateTime.getTime() + (5 * 60 * 1000)) > changedEndDateTime.getTime()) {
                return false;
            }
            //Check overlap at end
            if ((originalEndDateTime.getTime() - (5 * 60 * 1000)) < changedStartDateTime.getTime()) {
                return false;
            }

            return true;

        } else {
            // This ruleset is not handled!
            return false;
        }

    };

    $("body").on("change", ".staywithin-fields input", function (event) {
        $("#" + $(this).data("staywithin-validator")).valid();
    });
});
