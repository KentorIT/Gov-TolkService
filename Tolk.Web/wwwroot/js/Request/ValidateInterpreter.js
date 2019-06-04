var isValidatingInterpreter = false;
var $interpreterId;
var $officialInterpreterId;
var $competenceLevel;

var validateInterpreter = function (interpreterId, officialInterpreterId, competenceLevel, orderId) {
    if (competenceLevel === "" || competenceLevel === "OtherInterpreter" || interpreterId === "") {
        $('.interpreter-information').addClass("d-none");
    } else {
        $interpreterId = interpreterId;
        $officialInterpreterId = officialInterpreterId;
        $competenceLevel = competenceLevel;
        if (isValidatingInterpreter) {
            return;
        } else {
            isValidatingInterpreter = true;
            $('.interpreter-information').removeClass("d-none");
            $('.interpreter-information > span.info-message').text("Väntar på verifiering...");

            $('.interpreter-information').removeClass("warning-info-home").removeClass("system-action-info").addClass("system-message-warning")
                .children("span.glyphicon.message-icon").removeClass("glyphicon-exclamation-sign").removeClass("glyphicon-ok").addClass("glyphicon-hourglass");

            $('.interpreter-information > span.form-entry-information').addClass("d-none");
        }

        var $url = "";
        if (interpreterId === "-1") {
            $url = tolkBaseUrl + "Verify/InterpreterByOfficialId?officialInterpreterId=" + encodeURIComponent(officialInterpreterId) + "&orderId=" + orderId + "&competenceLevel=" + competenceLevel;
        } else {
            $url = tolkBaseUrl + "Verify/InterpreterByInternalId?id=" + interpreterId + "&orderId=" + orderId + "&competenceLevel=" + competenceLevel;
        }
        $.ajax({
            type: "GET",
            url: $url,
            dataType: "json",
            success: function (data) {
                $('.interpreter-information').removeClass("d-none");
                $('.interpreter-information > span.info-message').text(data.description);
                if (data.value === 100) {
                    $('.interpreter-information').removeClass("warning-info-home").removeClass("system-message-warning").addClass("system-action-info")
                        .children("span.glyphicon.message-icon").removeClass("glyphicon-exclamation-sign").removeClass("glyphicon-hourglass").addClass("glyphicon-ok");
                    $('.interpreter-information > span.form-entry-information').addClass("d-none");
                }
                else {
                    $('.interpreter-information').removeClass("system-action-info").removeClass("system-message-warning").addClass("warning-info-home")
                        .children("span.glyphicon.message-icon").removeClass("glyphicon-ok").removeClass("glyphicon-hourglass").addClass("glyphicon-exclamation-sign");
                    $('.interpreter-information > span.form-entry-information').removeClass("d-none");
                }
                isValidatingInterpreter = false;
                if ($interpreterId !== interpreterId ||
                    $officialInterpreterId !== officialInterpreterId ||
                    $competenceLevel !== competenceLevel) {
                    validateInterpreter($interpreterId, $officialInterpreterId, $competenceLevel, orderId);
                }
            },
            error: function (t2) {
                $('.interpreter-information').addClass("d-none");
            }
        });
    }
};