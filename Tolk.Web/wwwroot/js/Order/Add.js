
$(function () {
    var currentId = 0;

    {
        // Add ranking
        var i = 0;
        $("#competence-requested > .competence-level").each(function () {
            $(this).find(".select2").before("<div class=\"competence-ranking-num\">" + ++i + ".</span>");
        });
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
    $("body").on("click", ".add-requirement-button", function () {
        $($(this).data("target")).find("input:not(:checkbox),select, textarea").val("");
        $($(this).data("target")).find("input:checkbox").prop("checked", false);
        var $form = $($(this).data("target")).find('form:first');
        $($(this).data("target")).bindEnterKey('form:first input', '.btn-default');
        $form.find(".field-validation-error")
            .addClass("field-validation-valid")
            .removeClass("field-validation-error").html("");
    });
    $("body").on("click", ".save-requirement", function (event) {
        event.preventDefault();
        //Before we start, validate the form!
        if ($(this).parents(".modal-content").find("form").valid()) {
            var $hidden = $("#baseRequirement").clone();
            //Change the ids for the cloned inputs
            $hidden.find("input").each(function () {
                $(this).prop("id", $(this).prop("id").replace("0", currentId));
                $(this).prop("name", $(this).prop("name").replace("0", currentId));
            });
            currentId++;
            $(this).parents(".modal-content").find("input:not(:checkbox), select, textarea").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).val());
            });
            $(this).parents(".modal-content").find("input:checkbox").each(function () {
                $hidden.find("input[name$='" + $(this).prop("id") + "']").val($(this).is(":checked") ? "true" : "false");
            });
            //Add the info to the cloned hidden fields.
            //add a row to the table
            var isRequired = $("#RequirementIsRequired").is(":checked") ? "Ja" : "Nej";
            $('.order-requirement-table > tbody:last-child').append('<tr>' +
                '<td class="table-type-column">' + $hidden.html() + $("#RequirementType option:selected").text() + '</td>' +
                '<td class="table-description-column">' + $("#RequirementDescription").val() + '</td>' +
                '<td class="table-is-required-column">' + isRequired + '</td>' +
                '<td class="table-button-column fixed"><span class="glyphicon glyphicon-trash remove-requirement-row"></span></td>' +
                '</tr>');
            //Make the table visible, if this is the first visible row.
            $('.order-requirement-list').removeClass("d-none");
            //Close dialog
            $("#addRequirement").modal("hide");
        }
    });
    $("body").on("change", "#AssignmentType", function (event) {
        if ($(this).val() === "Education") {
            $('#language-panel').collapse('hide');
            toggleOtherLanguage("");
        }
        else {
            $('#language-panel').collapse('show');
            toggleOtherLanguage($("#LanguageId").val());
        }
    });

    $("#RankedInterpreterLocationFirst").change(function () { checkInterpreterLocation(); });
    $("#RankedInterpreterLocationSecond").change(function () { checkInterpreterLocation(); });
    $("#RankedInterpreterLocationThird").change(function () { checkInterpreterLocation(); });

    function checkInterpreterLocation() {
        var no1 = $("#RankedInterpreterLocationFirst").val();
        var no2 = $("#RankedInterpreterLocationSecond").val();
        var no3 = $("#RankedInterpreterLocationThird").val();
        var validator = $("#interpreterLocationValidator");
        if (no1 != "" && no1 == no2) {
            triggerValidator("Inställelsesätt i första och andra hand kan inte vara samma", validator);
        }
        else if (no1 != "" && no1 == no3) {
            triggerValidator("Inställelsesätt i första och tredje hand kan inte vara samma", validator);
        }
        else if (no2 != "" && no2 == no3) {
            triggerValidator("Inställelsesätt i andra och tredje hand kan inte vara samma", validator);
        }
        else {
            validator.hide();
            $('#send').attr('disabled', false);
        }
    }

    function triggerValidator(message, validatorId) {
        $('#send').attr('disabled', true);
        validatorId.empty();
        validatorId.append(message);
        validatorId.show();
    }

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

    var toggleOtherLanguage = function (id) {
        if (id === $("#OtherLanguageId").val()) {
            $('#other-language').collapse('show');
        }
        else {
            $('#other-language').collapse('hide');
        }
    };

    $("#SpecificCompetenceLevelRequired").trigger("change");
});
