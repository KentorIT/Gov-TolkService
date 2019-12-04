var LastAnswerByIsShowing = false;

$(function () {
    var currentId = 0;
    var currentDesiredId = 0;
    var currentOccasionId = 0;
    var hasToggledLastTimeForRequiringLatestAnswerBy = false;
    var allowLatestAnswerBy = true;

    var occasionButtons = '<div class="pull-right">' +
        '<a class="btn btn-danger small-button remove">Ta bort <span class="btn-remove-times-small">&times;</span></a>' +
        '</div>';

    var requiredModal = $("#req").parents(".modal-content");
    var desiredModal = $("#des").parents(".modal-content");

    var toggleLanguage = function (selectedItem) {
        if (selectedItem.attr('value') === $("#OtherLanguageId").val()) {
            $('#other-language').collapse('show');
        }
        else {
            $('#other-language').collapse('hide');
        }
        if (selectedItem.attr('data-additional') === "") {
            $('.competence-information > span.info-message-small').text("").removeAttr("role");
            $('#divNonCompetenceLevel').show();
            $('#divNonCompetenceLevel2').show();
            $('#divCompetenceLevel').hide();
            $('#LanguageHasAuthorizedInterpreter').val('false');
            $('.competence-information').removeClass("d-none");
            $('.competence-information').addClass("d-none");
            $("#competence-not-available").hide();
        }
        else {
            setCompetenceInfo(selectedItem.attr('data-additional'));
            $('#divNonCompetenceLevel').hide();
            $('#divNonCompetenceLevel2').hide();
            $('#divCompetenceLevel').show();
            $('#LanguageHasAuthorizedInterpreter').val('true');
            validateAvailableRequiredCompetences();
        }
    };

    var setCompetenceInfo = function (compLevelString) {
        $('.competence-information').removeClass("d-none");
        $('.competence-information > div.comp-list').find("li").remove();
        $('.competence-information > div.comp-list').find("ul").remove();

        if (compLevelString.length === 4) {
            $('.competence-information').removeClass("warning-info-home").removeClass("warning-info-yellow").addClass("system-action-info")
                .children("span.glyphicon.message-icon").removeClass("glyphicon-exclamation-sign").addClass("glyphicon-ok").removeClass("yellow-glyphicon");
            $('.competence-information > span.info-message-small').text("Det finns tolkar inom samtliga kompetensnivåer för valt språk i Kammarkollegiets tolkregister").attr("role", "status");
        }
        else if (compLevelString === "0") {
            $('.competence-information').removeClass("warning-info-yellow").removeClass("system-action-info").addClass("warning-info-home")
                .children("span.glyphicon.message-icon").removeClass("glyphicon-ok").addClass("glyphicon-exclamation-sign").removeClass("yellow-glyphicon");
            $('.competence-information > span.info-message-small').text("Det finns för närvarande inga tolkar som är utbildade eller auktoriserade i valt språk i Kammarkollegiets tolkregister").attr("role", "alert");
        }
        else {
            $('.competence-information').removeClass("warning-info-home").removeClass("system-action-info").addClass("warning-info-yellow")
                .children("span.glyphicon.message-icon").removeClass("glyphicon-ok").addClass("glyphicon-exclamation-sign").addClass("yellow-glyphicon");
            $('.competence-information > span.info-message-small').text("Tolkar med följande kompetensnivå finns i Kammarkollegiets tolkregister för valt språk:").attr("role", "alert");
            $('.competence-information > div.comp-list').append('<ul>');
            if (compLevelString.indexOf("L") >= 0) {
                $('.competence-information > div.comp-list').find("ul").append('<li>Rättstolk</li>');
            }
            if (compLevelString.indexOf("H") >= 0) {
                $('.competence-information > div.comp-list').find("ul").append('<li>Sjukvårdstolk</li>');
            }
            if (compLevelString.indexOf("A") >= 0) {
                $('.competence-information > div.comp-list').find("ul").append('<li>Auktoriserad tolk</li>');
            }
            if (compLevelString.indexOf("E") >= 0) {
                $('.competence-information > div.comp-list').find("ul").append('<li>Utbildad tolk</li>');
            }
            $('.competence-information > div.comp-list').append('</ul>');
        }
    };

    var toggleGender = function (reqType, genderSpan, descriptionSpan) {
        if (reqType.val() === "Gender") {
            genderSpan.show();
            descriptionSpan.hide();
        }
        else {
            genderSpan.hide();
            descriptionSpan.show();
            descriptionSpan.find("input").val("");
        }
    };

    var addOccasion = function () {
        $('#OccasionValidator').empty();
        if (!allowLatestAnswerBy) {
            triggerOrderValidator("Det går inte att lägga till tillfällen för nära i tiden när man vill boka flera tillfällen.", $('#OccasionValidator'));
            return;
        }
        if (!hasValidOccasion())
        {
            triggerOrderValidator("Fyll i datum och tid", $('#OccasionValidator'));
            return;
        }
        var occStartDateAndTime = getDate($("#SplitTimeRange_StartDate").val(), $("#SplitTimeRange_StartTimeHour").val(), $("#SplitTimeRange_StartTimeMinutes").val());
        var occEndDateAndTime = getDate($("#SplitTimeRange_StartDate").val(), $("#SplitTimeRange_EndTimeHour").val(), $("#SplitTimeRange_EndTimeMinutes").val());
        if (occEndDateAndTime < occStartDateAndTime) {
            occEndDateAndTime.addDays(1);
        }
        var validationMessage = checkEachOccasion(occStartDateAndTime, occEndDateAndTime);
        if (validationMessage !== "") {
            triggerOrderValidator(validationMessage, $('#OccasionValidator'));
            return;
        }
        var $form = $('.order-datepicker').closest('form');
        var validator = $form.validate();
        var valid = true;
        $('.order-datepicker input, .order-datepicker select').each(function (i, v) {
            valid = validator.element(v) && valid;
        });

        if (!valid) {
            return;
        }
        var hasExtra = $("#ExtraInterpreter").is(":checked") ? 'Ja' : 'Nej';
        var $hidden = $("#baseOccasion").clone();
        //Change the ids for the cloned inputs
        $hidden.find("input").each(function () {
            $(this).prop("id", $(this).prop("id").replace("0", currentOccasionId));
            $(this).prop("name", $(this).prop("name").replace("0", currentOccasionId));
        });
        currentOccasionId++;
        var date =
            $("#SplitTimeRange_StartDate").val() +
            ' ' +
            zeroPrefix($("#SplitTimeRange_StartTimeHour").val()) +
            ':' +
            zeroPrefix($("#SplitTimeRange_StartTimeMinutes").val()) +
            '-' +
            zeroPrefix($("#SplitTimeRange_EndTimeHour").val()) +
            ':' +
            zeroPrefix($("#SplitTimeRange_EndTimeMinutes").val());

        var start = new Date($("#SplitTimeRange_StartDate").val());
        start.setHours($("#SplitTimeRange_StartTimeHour").val());
        start.setMinutes($("#SplitTimeRange_StartTimeMinutes").val());
        $hidden.find("input[name$='OccasionStartDateTime']").val($("#SplitTimeRange_StartDate").val() +
            ' ' +
            zeroPrefix($("#SplitTimeRange_StartTimeHour").val()) +
            ':' +
            zeroPrefix($("#SplitTimeRange_StartTimeMinutes").val()));
        var end = new Date($("#SplitTimeRange_StartDate").val());
        end.setHours($("#SplitTimeRange_EndTimeHour").val());
        end.setMinutes($("#SplitTimeRange_EndTimeMinutes").val());
        if (end <= start) {
            end.addDays(1);
        }
        $hidden.find("input[name$='OccasionEndDateTime']").val(end.toLocaleDateString("sv-SE").replace(/&lrm;|\u200E/gi, '') +
            ' ' +
            zeroPrefix($("#SplitTimeRange_EndTimeHour").val()) +
            ':' +
            zeroPrefix($("#SplitTimeRange_EndTimeMinutes").val()));
        $hidden.find("input[name$='ExtraInterpreter']").val($("#ExtraInterpreter").is(":checked") ? "true" : "false");

        var table = $('.several-occasions-table table').DataTable();
        table.row.add([date + $hidden.html(), hasExtra, occasionButtons]).draw();

        $('.order-datepicker input:text').val('');
        $(".order-datepicker select").each(function (i, v) {
            $(this).val("").trigger("change");
        });
        $("#ExtraInterpreter").prop('checked', false);

    };

    var zeroPrefix = function (val) {
        return val < 10 ? "0" + val : "" + val;
    };

    var checkTimeAtStart = function () {
        var now = new Date($("#now").val());
        hasToggledLastTimeForRequiringLatestAnswerBy = !(now.getHours() === 13 || now.getHours() === 23);
    };

    var toggleSeveralOccasions = function () {
        //if date is set, and hasToggledLastTimeForRequiringLatestAnswerBy is false and SeveralOccasions is false
        if ($("#SeveralOccasions").is(":checked")) {
            return;
        }
        var $disabled = LastAnswerByIsShowing || !hasValidOccasion();
        $("#SeveralOccasions").prop('disabled', $disabled);
        if ($disabled) {
            $("#SeveralOccasions").parents(".checkbox").addClass("checkbox-disabled");
        } else {
            $("#SeveralOccasions").parents(".checkbox").removeClass("checkbox-disabled");
        }
    };

    var hasValidOccasion = function () {
        return !($("#SplitTimeRange_StartDate").val() === "" ||
            $("#SplitTimeRange_StartTimeHour").val() === "" ||
            $("#SplitTimeRange_StartTimeMinutes").val() === "" ||
            $("#SplitTimeRange_EndTimeHour").val() === "" ||
            $("#SplitTimeRange_EndTimeMinutes").val() === "");
    };

    function checkEachOccasion(start, end) {
        var now = new Date($("#now").val());
        if (now - start === 0 || now > start) {
            return "Tid och datum för tillfället har redan passerat.";
        }
        var message = "";
        var occTbody = $("#occasion-tbody");
        var $rows = occTbody.find("tr");
        if ($rows.length === 1 && currentOccasionId === 0) {
            return message;
        }
        else {
            $rows.each(function () {
                var tdStart = $(this).find("input[name$='OccasionStartDateTime']").val();
                var tdEnd = $(this).find("input[name$='OccasionEndDateTime']").val();
                //check added occasion against previous occasions
                if (new Date(tdStart) - end === 0 || new Date(tdEnd) - start === 0 || new Date(tdStart) - start === 0 || new Date(tdEnd) - end === 0) {
                    message = "Detta tillfälle startar eller slutar samtidigt som ett tidigare tillagt tillfälle startar eller slutar. Det måste vara mellanrum mellan tillfällena.";
                    return;
                }
                else if ((new Date(tdStart) > start && new Date(tdStart) < end) || (new Date(tdStart) < start && new Date(tdEnd) > start)) {
                    message = "Detta tillfälle överlappar med ett tidigare tillagt tillfälle.";
                    return;
                }
            });
            return message;
        }
    }

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
        toggleGender(target.find("#RequirementType"), target.find("input[name$=Gender]").parents(".form-group"), target.find("#RequirementDescription").parents(".form-group"));
    });

    $("body").on("click", ".add-desiredRequirement-button", function () {
        var target = $($(this).data("target"));
        AddRequirement(target);
        toggleGender(target.find("#DesiredRequirementType"), target.find("input[name$=DesiredGender]").parents(".form-group"), target.find("#DesiredRequirementDescription").parents(".form-group"));
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

        if (modalContent.find("#DesiredRequirementType option:selected").text() === "Tolkens kön") {
            var textToUse = modalContent.find("input[type='Radio']").filter(":checked").val() === "Female" ? "Kvinna" : "Man";
            modalContent.find("#DesiredRequirementDescription").val(textToUse);
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
                '<td class="table-type-column">' + $hidden.html() + $(this).parents(".modal-content").find("#DesiredRequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $(this).parents(".modal-content").find("#DesiredRequirementDescription").val() + '</td>' +
                '<td class="table-button-column fixed"><span class="remove-desiredRequirement-row bold">&times;</span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-desiredRequirement-list').removeClass("d-none");
            //Close dialog
            $("#addDesiredRequirement").modal("hide");
        }
    });

    requiredModal.find("#RequirementType").on("change", function () {
        toggleGender(requiredModal.find("#RequirementType"), requiredModal.find("input[name$=Gender]").parents(".form-group"), requiredModal.find("#RequirementDescription").parents(".form-group"));
    });

    desiredModal.find("#DesiredRequirementType").on("change", function () {
        toggleGender(desiredModal.find("#DesiredRequirementType"), desiredModal.find("input[name$=DesiredGender]").parents(".form-group"), desiredModal.find("#DesiredRequirementDescription").parents(".form-group"));
    });

    $("body").on("change", "#LanguageId", function () {
        toggleLanguage($("#LanguageId option:selected"));
    });

    $("body").on("change", "#RequestedCompetenceLevelFirst, #RequestedCompetenceLevelSecond", function () {
        validateAvailableRequiredCompetences();
    });

    function validateSelectedCompetences() {
        if ($("#LanguageId option:selected").attr('data-additional') !== "") {
            var competenceDesireType = $("input[name = CompetenceLevelDesireType]").filter('input:checked');
            var firstSelected = $("#RequestedCompetenceLevelFirst").val();
            var secondSelected = $("#RequestedCompetenceLevelSecond").val();
            if ($(competenceDesireType[0]).val() === 'Requirement') {
                if (firstSelected === '') {
                    return "Krav i första hand måste anges"
                }
                if (firstSelected === secondSelected) {
                    return "Krav i första och andra hand kan inte vara samma"
                }
            }
            if ($(competenceDesireType[0]).val() === 'Request') {
                if (firstSelected === '' && (firstSelected !== secondSelected)) {
                    return "Ange önskemål i första hand om du angett önskemål i andra hand"
                }
                if (firstSelected !== '' && (firstSelected === secondSelected)) {
                    return "Önskemål i första och andra hand kan inte vara samma"
                }
            }
        }
        return "";
    }

    $("body").on("change", "input[name=CompetenceLevelDesireType]", function () {
        var items = $(this).filter('input:checked');
        if ($(items[0]).val() === 'Request') {
            // Is request
            $("#competence-required, #competence-info-requirement, #competence-not-available").hide();
            $("#competence-requested, #competence-info, #competence-prio-list").show();
        }
        else if ($(items[0]).val() === 'Requirement') {
            // Is requirement
            $("#competence-requested, #competence-info").hide();
            $("#competence-required, #competence-prio-list, #competence-info-requirement").show();
            validateAvailableRequiredCompetences();
        }
        else {
            $("#competence-requested, #competence-required, #competence-prio-list, #competence-not-available, #competence-info-requirement").hide();
            $("#competence-info").show();
        }
    });

    function validateAvailableRequiredCompetences() {
        var currentLanguageCompetences = $("#LanguageId option:selected").attr('data-additional');
        var showWarning = false;
        //check if required is checked and if all competences not available - then validate
        var competenceDesireType = $("input[name = CompetenceLevelDesireType]").filter('input:checked');
        if ($(competenceDesireType[0]).val() === 'Requirement' && currentLanguageCompetences.length !== 4) {
            $('#RequestedCompetenceLevelFirst, #RequestedCompetenceLevelSecond').each(function () {
                switch ($(this).val()) {
                    case "CourtSpecialist":
                        if (!(currentLanguageCompetences.indexOf("L") >= 0)) {
                            showWarning = true;
                        }
                        break;
                    case "HealthCareSpecialist":
                        if (!(currentLanguageCompetences.indexOf("H") >= 0)) {
                            showWarning = true;
                        }
                        break;
                    case "AuthorizedInterpreter":
                        if (!(currentLanguageCompetences.indexOf("A") >= 0)) {
                            showWarning = true;
                        }
                        break;
                    case "EducatedInterpreter":
                        if (!(currentLanguageCompetences.indexOf("E") >= 0)) {
                            showWarning = true;
                        }
                        break;
                }
            });
        }
        if (showWarning) {
            $("#competence-not-available").show();
        }
        else {
            $("#competence-not-available").hide();
        }
    }

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

    $("body").on("change", "#SplitTimeRange_StartTimeHour, #SplitTimeRange_StartTimeMinutes, #SplitTimeRange_EndTimeHour, #SplitTimeRange_EndTimeMinutes", function () {
        toggleSeveralOccasions();
    });

    $("body").on("change", "#LatestAnswerBy_Hour", function () {
        if ($("#LatestAnswerBy_Minute").val() === "") {
            $("#LatestAnswerBy_Minute").val(0).trigger("change").trigger("select2:select");
        }
    });

    $("body").on("change", "#SplitTimeRange_StartDate", function () {
        var now = new Date($("#now").val());
        if (!hasToggledLastTimeForRequiringLatestAnswerBy && (now.getHours() === 14 || now.getHours() === 0)) {
            $("#LastTimeForRequiringLatestAnswerBy").val($("#NextLastTimeForRequiringLatestAnswerBy").val());
            hasToggledLastTimeForRequiringLatestAnswerBy = true;
        }
        var lastTimeForRequiringLatestAnswerBy = new Date($("#LastTimeForRequiringLatestAnswerBy").val());
        var chosenDate = new Date($(this).val());
        allowLatestAnswerBy = true;
        if (chosenDate <= lastTimeForRequiringLatestAnswerBy) {
            if ($("#SeveralOccasions").is(":checked")) {
                //NOT ALLOWED!!! Mark date as invalid, with the message that occasions cannot be to close in time if one wants to register several occasions...
                //POSSIBLY CHANGE THE FIRST ALLOWED DATE?
                allowLatestAnswerBy = false;
                return;
            }
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
        toggleSeveralOccasions();
    });

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

    $("body").on("click", "tr > td > div > a.small-button.remove", function () {
        var table = $('.several-occasions-table table').DataTable();
        table.row($(this).parents('tr')).remove().draw();
        //if last row was removed, uncheck the severaloccasions
        currentOccasionId = 0;
        if (!table.data().any()) {
            $("#SeveralOccasions").prop("checked", false).trigger("change");
            toggleSeveralOccasions();
        } else {
            $('.several-occasions-table table tbody tr').each(function () {
                $(this).find("input").each(function () {
                    var $id = $(this).prop("id").match(/\d+/);
                    $(this).prop("id", $(this).prop("id").replace($id, currentOccasionId));
                    $(this).prop("name", $(this).prop("name").replace($id, currentOccasionId));
                });
                currentOccasionId++;
            });
        }
    });

    $("body").on("change", "#SeveralOccasions", function () {
        if ($(this).is(":checked")) {
            $(".several-occasions-table").show();
            $(".add-date-button-row").show();
            addOccasion();
        } else {
            $("#OccasionValidator").show();
            $("#OccasionValidator").empty();
            $(".several-occasions-table").hide();
            $(".add-date-button-row").hide();
            var table = $('.several-occasions-table table').DataTable();
            table.clear().draw();
            currentOccasionId = 0;

            if ($("#SplitTimeRange_StartDate").val() !== "") {
                //Check if last answer by should be shown...
                $("#SplitTimeRange_StartDate").trigger("change");
            }
            toggleSeveralOccasions();
        }
    });

    $("body").on("click", ".add-occasion", function () {
        addOccasion();
    });

    //At start 
    $(".allow-more-travel-cost").hide();
    $("input[name=CompetenceLevelDesireType]").trigger("change");
    $("#UseRankedInterpreterLocation").trigger("change");
    checkTimeAtStart();
    $("#SplitTimeRange_StartDate").trigger("change");
    $(".allow-no-review-travel-cost-information").hide();
    $(".allow-more-travel-cost-information").hide();
    $("#SeveralOccasions").trigger("change");
    $(".extra-interpreter-part").detach().appendTo(".date-and-time-part");
    $("#SeveralOccasions").prop('disabled', true);

    function AddRequirement(target) {
        target.find("#RequirementDescription").val("");
        var $form = target.find('form:first');
        target.bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
    }

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

    function validatorMessage(forName, message) {
        var validatorQuery = "[data-valmsg-for=\"" + forName + "\"]";
        $(validatorQuery).empty();
        $(validatorQuery).append(message);
        $(validatorQuery).show();
    }
    var $this = $(".wizard");
    $this.tolkWizard({
        backHandler: function (event) {
            $("#send").tooltip("destroy");
        },
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
                validatorMessage("AllowExceedingTravelCost", "Ange huruvida restid eller resväg som överskriver gränsvärden accepteras");
                errors++;
            }
            var competenceMessage = validateSelectedCompetences();
            if (competenceMessage !== "") {
                validatorMessage("RequestedCompetenceLevelFirst", competenceMessage);
                errors++;
            }
            if (!$("#SeveralOccasions").is(":checked")) {
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
            } else {
                //Check if there is a valid, not yet added, occasion, and if ask if the user wants to add it or not.
                if (hasValidOccasion()) {
                    if (!confirm("Det finns ett fullständigt tillfälle som inte är tillagt än. Vill du fortsätta?")) {
                        return false;
                    }
                }
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
                        $("#send").removeAttr("disabled")
                            .tooltip({ title: "Observera att bokningen inte kan eller får ändras efter att den skickats iväg. Om du är osäker på ifall det går att hitta en tolk som uppfyller ställda krav kan du istället ange dem som önskemål" });
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

    function triggerOrderValidator(message, validatorId) {
        validatorId.empty();
        validatorId.append(message);
        validatorId.show();
    }

    $("body").on("mousedown", ".wizard-forward-button", function () {
        //This is done to make sure that the wizard validation does not validate date add stuff when it shouldn't
        //This would be better handled with a beforeNextHandler in wizard, or some other name...
        if ($("#SeveralOccasions").is(":checked")) {
            $('.order-datepicker input, .order-datepicker select').each(function (i, v) {
                $(this).addClass("ignore-validation");
            });
        }
    });

    $("body").on("click", ".wizard-forward-button", function () {
        $('.order-datepicker input, .order-datepicker select').each(function (i, v) {
            $(this).removeClass("ignore-validation");
        });
    });
});
