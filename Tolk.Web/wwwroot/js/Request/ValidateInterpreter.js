var validateInterpreter = function () {
    if ($("#InterpreterCompetenceLevel").val() === "" || $("#InterpreterCompetenceLevel").val() === "OtherInterpreter" ||
        $('#InterpreterId').val() === "") {
        $('.interpreter-information').addClass("d-none");
    } else {
        var $url = "";
        if ($('#InterpreterId').val() === "-1") {
            $url = tolkBaseUrl + "Verify/InterpreterByOfficialId?officialInterpreterId=" + encodeURIComponent($('#NewInterpreterOfficialInterpreterId').val()) + "&orderId=" + $("#OrderId").val() + "&competenceLevel=" + $("#InterpreterCompetenceLevel").val();
        } else {
            $url = tolkBaseUrl + "Verify/InterpreterByInternalId?id=" + $('#InterpreterId').val() + "&orderId=" + $("#OrderId").val() + "&competenceLevel=" + $("#InterpreterCompetenceLevel").val();
        }
        $.ajax({
            type: "GET",
            url: $url,
            dataType: "json",
            success: function (data) {
                //system-action-info || warning-info-home
                //glyphicon-ok || glyphicon-exclamation-sign
                $('.interpreter-information').removeClass("d-none");
                $('.interpreter-information > span.info-message').text(data.description);
                if (data.value === 100) {
                    $('.interpreter-information').removeClass("warning-info-home").addClass("system-action-info")
                        .children("span.glyphicon").removeClass("glyphicon-exclamation-sign").addClass("glyphicon-ok");
                }
                else {
                    $('.interpreter-information').removeClass("system-action-info").addClass("warning-info-home")
                        .children("span.glyphicon").removeClass("glyphicon-ok").addClass("glyphicon-exclamation-sign");
                }
            },
            error: function (t2) {
                $('.interpreter-information').addClass("d-none");
            }
        });
    }
};