var LastAnswerByIsShowing = false;

$(function () {
    var currentId = 0;
    var currentDesiredId = 0;

    $(".allow-more-travel-cost").hide();

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
        toggleGender(target.find("#RequirementType"), target.find("#GenderRequirement"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".add-desiredRequirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#RequirementType"), target.find("#GenderRequest"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".save-requirement", function (event) {
        event.preventDefault();
        var modalContent = $(this).parents(".modal-content");

        if (modalContent.find("#RequirementType option:selected").text() === "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() === "Female" ? "Kvinna" : "Man";
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

        if (modalContent.find("#RequirementType option:selected").text() === "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() === "Female" ? "Kvinna" : "Man";
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
        toggleGender(requiredModal.find("#RequirementType"), requiredModal.find("#GenderRequirement"), requiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    desiredModal.find("#RequirementType").on("change", function () {
        toggleGender(desiredModal.find("#RequirementType"), desiredModal.find("#GenderRequest"), desiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("change", "#LanguageId", function () {
        toggleLanguage($("#LanguageId option:selected"));
    });

    $("body").on("change", "input[name=CompetenceLevelDesireType]", function () {
        var items = $(this).filter('input:checked');
        if ($(items[0]).val() === 'Request') {
            // Is request
            $("#competence-required").hide();
            $("#competence-requested").show();
            $("#competence-info").show();
        }
        else if ($(items[0]).val() === 'Requirement') {
            // Is requirement
            $("#competence-requested").hide();
            $("#competence-required").show();
            $("#competence-info").hide();
        }
        else {
            $("#competence-requested").hide();
            $("#competence-required").hide();
            $("#competence-info").show();
        }
    });

    $("body").on("change", "#SplitTimeRange_StartTimeHour", function () {
        var chosenStartHour = parseInt($(this).val());
        var chosenEndHour = $("#SplitTimeRange_EndTimeHour").val();
        var chosenStartMinutes = $("#SplitTimeRange_StartTimeMinutes").val();
        var chosenEndMinutes = $("#SplitTimeRange_EndTimeMinutes").val();
        //only set minutes and endhour if not selected yet
        if (chosenEndHour === "") {
            var nextHour = parseInt($(this).val()) === 23 ? 0 : chosenStartHour + 1;
            $("#SplitTimeRange_EndTimeHour").val(nextHour).trigger("change").trigger("select2:select");
        }
        if (chosenStartMinutes === "") {
            $("#SplitTimeRange_StartTimeMinutes").val(0).trigger("change").trigger("select2:select");
        }
        if (chosenEndMinutes === "") {
            $("#SplitTimeRange_EndTimeMinutes").val(0).trigger("change").trigger("select2:select");
        }
    });

    $("body").on("change", "#LatestAnswerBy_Hour", function () {    
        if ($("#LatestAnswerBy_Minute").val() === "") {
            $("#LatestAnswerBy_Minute").val(0).trigger("change").trigger("select2:select");
        }
    });

    var hasToggledLastTimeForRequiringLatestAnswerBy = false;

    $("body").on("change", "#SplitTimeRange_StartDate", function () {
        var now = new Date($("#now").val());
        if (!hasToggledLastTimeForRequiringLatestAnswerBy && (now.getHours() === 14 || now.getHours() === 0)) {
            $("#LastTimeForRequiringLatestAnswerBy").val($("#NextLastTimeForRequiringLatestAnswerBy").val());
            hasToggledLastTimeForRequiringLatestAnswerBy = true;
        }
        var lastTimeForRequiringLatestAnswerBy = new Date($("#LastTimeForRequiringLatestAnswerBy").val());
        var chosenDate = new Date($(this).val());
        if (chosenDate <= lastTimeForRequiringLatestAnswerBy) {
            $("#LatestAnswerBy").show();
            $("#LatestAnswerBy_Date").datepicker("setStartDate", now.zeroTime());
            $("#LatestAnswerBy_Date").datepicker("setEndDate", chosenDate);
            //If LastAnswerBy_Date === chosenDate, check the time-fields as well
            // Check both on change of LastAnswerBy_* and SplitTimeRange_*
            LastAnswerByIsShowing = true;
        }
        else {
            $("#LatestAnswerBy").hide();
            LastAnswerByIsShowing = false;
        }
    });

    var toggleLanguage = function (selectedItem) {
        if (selectedItem.attr('value') === $("#OtherLanguageId").val()) {
            $('#other-language').collapse('show');
        }
        else {
            $('#other-language').collapse('hide');
        }
        if (selectedItem.attr('data-additional') === "") {
            $('#divNonCompetenceLevel').show();
            $('#divCompetenceLevel').hide();
            $('#LanguageHasAuthorizedInterpreter').val('false');
        }
        else {
            $('#divNonCompetenceLevel').hide();
            $('#divCompetenceLevel').show();
            $('#LanguageHasAuthorizedInterpreter').val('true');
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
            $(".allow-more-travel-cost").show();
        }
        else {
            $(".allow-more-travel-cost").hide();
        }
    });

    $("body").on("click", ".wizard-forward-button", function () {
        document.body.scrollTop = 0;
        document.documentElement.scrollTop = 0;
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

    $("body").on("click", "input[name=AllowExceedingTravelCost]", function () {
        if ($(this).val() === "YesShouldBeApproved") {
            $(".allow-more-travel-cost-information").show();
            $(".allow-no-review-travel-cost-information").hide();
        } else if ($(this).val() === "YesShouldNotBeApproved") {
            $(".allow-more-travel-cost-information").hide();
            $(".allow-no-review-travel-cost-information").show();
        } else {
            $(".allow-more-travel-cost-information").hide();
            $(".allow-no-review-travel-cost-information").hide();
        }
    });

    var checkTimeAtStart = function () {
        var now = new Date($("#now").val());
        hasToggledLastTimeForRequiringLatestAnswerBy = !(now.getHours() === 13 || now.getHours() === 23);
    };

    $("input[name=CompetenceLevelDesireType]").trigger("change");
    $("#UseRankedInterpreterLocation").trigger("change");
    checkTimeAtStart();
    $("#SplitTimeRange_StartDate").trigger("change");
    $(".allow-no-review-travel-cost-information").hide();
    $(".allow-more-travel-cost-information").hide();
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
            var startdate = new Date($("#SplitTimeRange_StartDate").val());
            var starthour = $("#SplitTimeRange_StartTimeHour").val();
            var startminute = $("#SplitTimeRange_StartTimeMinutes").val();
            if (date.equalsDate(new Date(startdate))) {
                if (Number(hour) > Number(starthour)) {
                    return false;
                } else if (Number(hour) === Number(starthour)) {
                    return (Number(startminute) > Number(minute));
                }
            }
        }

        return true;
    };

    var validateStartTime = function () {
        var date = new Date($("#SplitTimeRange_StartDate").val());
        var startHour = $("#SplitTimeRange_StartTimeHour").val();
        var startMinute = $("#SplitTimeRange_StartTimeMinutes").val();
        if (date !== "" && startHour !== "" && startMinute !== "") {
            var now = new Date($("#now").val());
            if (date.equalsDate(now)) {
                var hours = now.getHours();
                if (hours > Number(startHour)) {
                    return false;
                } else if (hours === Number(startHour)) {
                    return !(now.getMinutes() > Number(startMinute));
                }
            }
        }

        return true;
    };

    var validateStartTimeAndEndTime = function () {
        var startHour = Number($("#SplitTimeRange_StartTimeHour").val());
        var startMinute = Number($("#SplitTimeRange_StartTimeMinutes").val());
        var endHour = Number($("#SplitTimeRange_EndTimeHour").val());
        var endMinute = Number($("#SplitTimeRange_EndTimeMinutes").val());
        if (Number(startHour) === endHour) {
            return endMinute !== Number(startMinute);
        }
        return true;
    };
    var validateSelectedCompetenceLevelDesireType = function () {
        return $("#CompetenceLevelDesireType").is(":hidden") ||
            $("[name=CompetenceLevelDesireType]").filter(":checked").length > 0;
    };

    var validateAllowExceedingTravelCost = function () {
        if ($("#AllowExceedingTravelCost").is(":hidden")) {
            return true;
        }
        var checked = $("[id^=AllowExceedingTravelCost_]").filter(":checked")[0];
        return checked !== undefined;
    };

    var $this = $(".wizard");
    $this.tolkWizard({
        nextHandler: function (event) {
            var errors = 0;
            $("[data-valmsg-for]").empty();
            if (!LastAnswerByIsShowing) {
                $("#LatestAnswerBy_Date").val("");
                $("#LatestAnswerBy_Hour").val("").trigger('change');
                $("#LatestAnswerBy_Minute").val("").trigger('change');
            }
            if (!validateSelectedCompetenceLevelDesireType()) {
                validatorMessage("CompetenceLevelDesireType", "Ange om kompetensnivå är krav eller önskemål");
                errors++;
            }
            if (!validateAllowExceedingTravelCost()) {
                validatorMessage("AllowExceedingTravelCost", "Ange hurvida restid eller resväg som överskriver gränsvärden accepteras");
                errors++;
            }
            if (!validateStartTime()) {
                validatorMessage("SplitTimeRange.EndTimeMinutes", "Uppdraget har en starttid som redan har passerats, var god ändra detta.");
                errors++;
            }
            if (!validateStartTimeAndEndTime()) {
                validatorMessage("SplitTimeRange.EndTimeMinutes", "Uppdragets start- och sluttid har samma värde, var god ändra detta.");
                errors++;
            }
            if (!validateLastAnswerBy()) {
                validatorMessage("LatestAnswerBy.Date", "Sista svarstid har redan passerats, var god ändra detta.");
                errors++;
            }
            if (!validateLastAnswerByAgainstStartTime()) {
                validatorMessage("LatestAnswerBy.Date", "Sista svarstid kan inte vara senare än tolkuppdragets starttid, var god ändra detta.");
                errors++;
            }
            if (errors !== 0) {
                return false;
            }
            var $form = $this.closest('form');
            var currentStep = event.NextStep;
            $("#send").attr("disabled", "disabled");
            $("#back").attr("disabled", "disabled");
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
                        $('.form-entry-information').tooltip();
                        $("#send").removeAttr("disabled");
                        $("#back").removeAttr("disabled");
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
        }
    });
});

function validatorMessage(forName, message) {
    var validatorQuery = "[data-valmsg-for=\"" + forName + "\"]";
    $(validatorQuery).empty();
    $(validatorQuery).append(message);
    $(validatorQuery).show();
}