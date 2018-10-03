$(function () {
    $.validator.setDefaults({
        ignore: ":hidden:not(.force-validation)"
    });

    $.validator.addMethod("staywithin", function (value, element, options) {
        var thisPrefix = element.id.split('_')[0];
        return test(thisPrefix, $(element).data("rule-otherproperty"));
    }, $.validator.format("Not within the provided time range"));

    var test = function (thisPrefix, otherPrefix) {
        var dateField = new Date($("#" + otherPrefix + "_StartDate").val()).toLocaleDateString("sv-SE");
        var startField = $("#" + otherPrefix + "_StartTime").val();
        var endField = $("#" + otherPrefix + "_EndTime").val();
        var isTwoDay = startField > endField;
        var nextDay = new Date($("#" + otherPrefix + "_StartDate").val());
        nextDay.setDate(nextDay.getDate() + 1);
        var nextDayField = nextDay.toLocaleDateString("sv-SE");
        //Find the TimeRange_ part of the current input's name, to find the other inputs
        var dateFieldChanged = new Date($("#" + thisPrefix + "_StartDate").val()).toLocaleDateString("sv-SE");
        var startFieldChanged = $("#" + thisPrefix + "_StartTime").val();
        var endFieldChanged = $("#" + thisPrefix + "_EndTime").val();
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
    };

    $("body").on("change", ".staywithin-fields input", function (event) {
        $("#" + $(this).data("staywithin-validator")).valid();
    });
});
