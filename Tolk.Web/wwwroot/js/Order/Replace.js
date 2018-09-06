
$(function () {
    $.validator.addMethod("insideTime", function (value, element, params) {
        var dateFieldChanged = new Date($("#TimeRange_StartDate").val());
        var startFieldChanged = $("#TimeRange_StartTime").val();
        var endFieldChanged = $("#TimeRange_EndTime").val();
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

    },
        "Ersättningsuppdraget måste påbörjas och avslutas inom det uppdrag det ersätter.");
    $('form').validate({
        rules: {
            "TimeRange.StartDate": "insideTime",
            "TimeRange.StartTime": "insideTime",
            "TimeRange.EndTime": "insideTime",
            messages: {
                insideTime: "Ooops"
            }
        }
    });

    var dateField = new Date($("#TimeRange_StartDate").val());
    var startField = $("#TimeRange_StartTime").val();
    var endField = $("#TimeRange_EndTime").val();
    var isTwoDay = startField > endField;
    var nextDay = new Date($("#TimeRange_StartDate").val());
    nextDay.setDate(nextDay.getDate() + 1);
});
