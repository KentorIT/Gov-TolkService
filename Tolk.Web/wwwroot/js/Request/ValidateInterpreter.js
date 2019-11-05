var isValidatingInterpreter = false;
var $interpreterPanel;
var $interpreterId;
var $officialInterpreterId;
var $competenceLevel;

var validateInterpreter = function (interpreterPanel, interpreterId, officialInterpreterId, competenceLevel, orderId, orderGroupId) {
    var $infoPanel = $(interpreterPanel + ' .interpreter-information');
    var $infoMessage = $(interpreterPanel + ' .interpreter-information > span.info-message');
    var $infoBox = $(interpreterPanel + ' .interpreter-information > span.form-entry-information');
   if (competenceLevel === undefined || competenceLevel === "" || competenceLevel === "OtherInterpreter" || interpreterId === "") {
       $infoMessage.removeAttr("role");
       $infoPanel.addClass("d-none");
    } else {
        $interpreterId = interpreterId;
        $officialInterpreterId = officialInterpreterId;
        $competenceLevel = competenceLevel;
        $interpreterPanel = interpreterPanel;
        if (isValidatingInterpreter) {
            return;
        } else {
            isValidatingInterpreter = true;
            $infoPanel.removeClass("d-none");
            $infoMessage.text("Väntar på verifiering...");

            $infoPanel.removeClass("warning-info-home").removeClass("system-action-info").addClass("system-message-warning")
                .children("span.glyphicon.message-icon").removeClass("glyphicon-exclamation-sign").removeClass("glyphicon-ok").addClass("glyphicon-hourglass");

            $infoBox.addClass("d-none");
        }

        var $url = "";
        if (interpreterId === "-1") {
            $url = tolkBaseUrl + "Verify/InterpreterByOfficialId?officialInterpreterId=" + encodeURIComponent(officialInterpreterId) + "&competenceLevel=" + competenceLevel + "&orderId=" + orderId + "&orderGroupId=" + orderGroupId;
        } else {
            $url = tolkBaseUrl + "Verify/InterpreterByInternalId?id=" + interpreterId + "&competenceLevel=" + competenceLevel + "&orderId=" + orderId + "&orderGroupId=" + orderGroupId;
        }
        $.ajax({
            type: "GET",
            url: $url,
            dataType: "json",
            success: function (data) {
                $infoPanel.removeClass("d-none");
                if (data.value === 100) {
                    $infoMessage.text(data.description).attr("role", "status");
                    $infoPanel.removeClass("warning-info-home").removeClass("system-message-warning").addClass("system-action-info")
                        .children("span.glyphicon.message-icon").removeClass("glyphicon-exclamation-sign").removeClass("glyphicon-hourglass").addClass("glyphicon-ok");
                    $infoBox.addClass("d-none");
                }
                else {
                    $infoMessage.text(data.description).attr("role", "alert");
                    $infoPanel.removeClass("system-action-info").removeClass("system-message-warning").addClass("warning-info-home")
                        .children("span.glyphicon.message-icon").removeClass("glyphicon-ok").removeClass("glyphicon-hourglass").addClass("glyphicon-exclamation-sign");
                    $infoBox.removeClass("d-none");
                }
                isValidatingInterpreter = false;
                if ($interpreterId !== interpreterId ||
                    $officialInterpreterId !== officialInterpreterId ||
                    $competenceLevel !== competenceLevel ||
                    $interpreterPanel !== interpreterPanel) {
                    validateInterpreter($interpreterPanel, $interpreterId, $officialInterpreterId, $competenceLevel, orderId);
                }
            },
            error: function (t2) {
                $infoPanel.addClass("d-none");
            }
        });
    }
};