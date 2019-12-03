$(function () {
    var validatorMessage = function (forName, message) {
        var validatorQuery = "[data-valmsg-for=\"" + forName + "\"]";
        $(validatorQuery).empty();
        $(validatorQuery).append(message);
        $(validatorQuery).show();
    };
    var validateLastAnswerBy = function () {
        if (!$("#LatestAnswerBy_Date").is(":visible")) {
            return true;
        }
        var date = new Date($("#LatestAnswerBy_Date").val());
        var hour = $("#LatestAnswerBy_Hour").val();
        var minute = $("#LatestAnswerBy_Minute").val();
        if (date !== "" && hour !== "" && minute !== "") {
            var now = new Date($("#now").val());
            if (date.equalsDate(now)) {
                var hours = now.getHours();
                if (hours > Number(hour)) {
                    return false;
                } else if (hours === Number(hour)) {
                    return !(now.getMinutes() > Number(minute));
                }
            }
        }

        return true;
    };
    var validateLastAnswerByAgainstStartTime = function () {
        if (!$("#LatestAnswerBy_Date").is(":visible")) {
            return true;
        }
        var date = new Date($("#LatestAnswerBy_Date").val());
        var hour = $("#LatestAnswerBy_Hour").val();
        var minute = $("#LatestAnswerBy_Minute").val();
        if (date !== "" && hour !== "" && minute !== "") {
            var startDateTime = new Date($("#StartAt").val());
            var latestAnswerByDateTime = new Date(date);
            latestAnswerByDateTime.setHours(Number(hour));
            latestAnswerByDateTime.setMinutes(Number(minute));
            return latestAnswerByDateTime <= startDateTime;
        }

        return true;
    };

    if ($('#StartAt').length > 0) {
        // Turn datetime string into UTC string for parsing
        var startVal = $('#StartAt').val().replace(" ", "T").replace(" ", "");
        $('#StartAt').val(startVal);
        var now = new Date($('#now').val()).zeroTime();
        var start = new Date(startVal).zeroTime();
        $("#LatestAnswerBy_Date").datepicker("setStartDate", now);
        $("#LatestAnswerBy_Date").datepicker("setEndDate", start);
    }

    $("body").on("click", "#updateLatestAnswerBy", function (event) {
        // Validate LatestAnswerBy time
        if (!validateLastAnswerBy()
            || !validateLastAnswerByAgainstStartTime()) {
            event.preventDefault();
            validatorMessage("LatestAnswerBy.Date", "Ogiltig tid, vänligen kontrollera sista svarstid.");
        }
    });
});
