
$(function () {
    var currentId = 0;
    var currentDesiredId = 0;

    $("#travel-time-checkbox").hide();

    $("body").on("click", ".remove-requirement-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentId = 0;
        if ($rows.length === 0) {
            $('.order-requirement-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentId));
                });
                currentId++;
            });
        }
    });

    $("body").on("click", ".remove-desiredRequirement-row", function () {
        var $tbody = $(this).closest("tbody");
        $(this).closest("tr").remove();
        //Reindex 0 to n
        var $rows = $tbody.find("tr");
        currentDesiredId = 0;
        if ($rows.length === 0) {
            $('.order-desiredRequirement-list').addClass("d-none");
        } else {
            $rows.each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentDesiredId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentDesiredId));
                });
                currentDesiredId++;
            });
        }
    });

    $("body").on("click", ".add-requirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#RequirementType"), target.find("#Gender_rbGroup"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".add-desiredRequirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#RequirementType"), target.find("#Gender_rbGroup"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".save-requirement", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        if (modalContent.find("#RequirementType option:selected").text() == "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() == "Female" ? "Kvinna" : "Man";
            modalContent.find("#RequirementDescription").val(textToUse);
        }
        //Before we start, validate the form!
        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                $(this).prop("name", $(this).prop("name").replace("0", currentId));
            });
            currentId++;
            modalContent.find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            modalContent.find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });
            //Add the info to the cloned hidden fields, add a row to the table
            $('.order-requirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $("#RequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $("#RequirementDescription").val() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-requirement-row bold">&times;</span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-requirement-list').removeClass("d-none");
            //Close dialog
            $("#addRequirement").modal("hide");
        }
    });

    $("body").on("click", ".save-desiredRequirement", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        if (modalContent.find("#RequirementType option:selected").text() == "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() == "Female" ? "Kvinna" : "Man";
            modalContent.find("#RequirementDescription").val(textToUse);
        }
        //Before we start, validate the form!
        if (modalContent.find("form").valid()) {
            var $hidden = $("#baseDesiredRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentDesiredId));
                $(this).prop("name", $(this).prop("name").replace("0", currentDesiredId));
            });
            currentDesiredId++;
            modalContent.find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            modalContent.find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });

            //Add the info to the cloned hidden fields, add a row to the table
            $('.order-desiredRequirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $(this).parents(".modal-content").find("#RequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $(this).parents(".modal-content").find("#RequirementDescription").val() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-desiredRequirement-row bold">&times;</span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-desiredRequirement-list').removeClass("d-none");
            //Close dialog
            $("#addDesiredRequirement").modal("hide");
        }
    });

    var requiredModal = $("#req").parents(".modal-content");
    var desiredModal = $("#des").parents(".modal-content");

    requiredModal.find("#RequirementType").on("change", function () {
        toggleGender(requiredModal.find("#RequirementType"), requiredModal.find("#Gender_rbGroup"), requiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    desiredModal.find("#RequirementType").on("change", function () {
        toggleGender(desiredModal.find("#RequirementType"), desiredModal.find("#Gender_rbGroup"), desiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("change", "#LanguageId", function () {
        toggleOtherLanguage($(this).val());
    });

    $("body").on("change", "#SpecificCompetenceLevelRequired", function () {
        if (this.checked) {
            // Is requirement
            $("#competence-requested").hide();
            $("#competence-required").show();
        }
        else {
            // Is request
            $("#competence-required").hide();
            $("#competence-requested").show();
        }
    });

   $("body").on("change","#SplitTimeRange_StartTimeHour", function () {
        var chosenStartHour = parseInt($(this).val());
        var chosenEndHour = $("#SplitTimeRange_EndTimeHour").val();
        //only set endhour if not selected yet
        if (chosenEndHour == "") {
            var nextHour = parseInt($(this).val()) == 23 ? 0 : chosenStartHour + 1;
            $("#SplitTimeRange_EndTimeHour").val(nextHour).trigger("change");
        }
    });

    $("body").on("change", "#SplitTimeRange_StartDate", function () {
        var systemTime = new Date(Number($("#SystemTime").val()));
        var chosenDate = new Date($(this).val());
        var tomorrow = new Date(systemTime.getTime())
            .addDays(1)
            .zeroTime()
            .localDateTime();
        if (chosenDate.equalsDate(tomorrow) && systemTime.getHours() >= 14) {
            var today = new Date(systemTime.getTime())
                .zeroTime()
                .localDateTime();
            $("#LatestAnswerBy").show();
            $("#LatestAnswerBy_Date").datepicker("setStartDate", today);
            $("#LatestAnswerBy_Date").datepicker("setEndDate", tomorrow);
        }
        else {
            $("#LatestAnswerBy").hide();
        }
    });

    var toggleOtherLanguage = function (id) {
        if (id === $("#OtherLanguageId").val()) {
            $('#other-language').collapse('show');
        }
        else {
            $('#other-language').collapse('hide');
        }
    };

    var toggleGender = function (reqType, genderRbl, description) {
        if (reqType.val() === "Gender") {
            genderRbl.show();
            description.hide();
        }
        else {
            genderRbl.hide();
            description.show();
            description.find("#RequirementDescription").val("");
        }
    };

    $("body").on("change", ".location-group", function () {
        var isOnsiteSelected = false;
        $("select[id^=RankedInterpreterLocation]").each(function () {
            switch ($(this).val()) {
                default:
                    break;
                case "OnSite":
                case "OffSiteDesignatedLocation":
                    isOnsiteSelected = true;
                    break;
            }
        });

        if (isOnsiteSelected) {
            $("#travel-time-checkbox").show();
        }
        else {
            $("#travel-time-checkbox").hide();
        }
    });

    $("body").on("change", "#RequiredCompetenceLevels", function () {
        var allCheckboxes = $('[data-checkbox-group="RequiredCompetenceLevels"]');
        var checkedBoxes = allCheckboxes.filter(':checked');

        if (checkedBoxes.length >= 2) {
            allCheckboxes.filter(':not(:checked)').attr('disabled', 'disabled');
        }
        else {
            allCheckboxes.filter(':not(:checked)').removeAttr('disabled');
        }
    });

    $("#SpecificCompetenceLevelRequired").trigger("change");
    $("#UseRankedInterpreterLocation").trigger("change");
    $("#SplitTimeRange_StartDate").trigger("change");
});

function AddRequirement(target) {
    target.find("#RequirementDescription").val("");
    var $form = target.find('form:first');
    target.bindEnterKey('form:first input', '.btn-default');
    $form.find(".field-validation-error")
        .addClass("field-validation-valid")
        .removeClass("field-validation-error").html("");
}

$(function () {
    var $this = $(".wizard");
    $this.tolkWizard({
        nextHandler: function (event) {
            var $form = $this.closest('form');
            var currentStep = event.NextStep;
            if (event.IsLastPage) {
                $form.submit();
            }
            //post to confirm
            else {
                var $url = tolkBaseUrl + "Order/Confirm";
                $.ajax({
                    url: $url,
                    type: 'POST',
                    data: $form.serialize(),
                    dataType: 'html',
                    success: function (data) {
                        $(".wizard .wizard-step").eq(currentStep).html(data);
                    },
                    error: function (t2) {
                        alert(t2);
                    }
                });
            }
        },
        wizardStepRendered: function () {
            $("#send").append('<span class="center-glyphicon glyphicon glyphicon-triangle-right"></span>');
            $("#send").blur();
        },
    });
});