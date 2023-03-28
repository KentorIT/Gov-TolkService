$.fn.extend({

    wizard: function (opts) {

        var $wizard = $(this);
        var numberOfSteps = $wizard.find(".wizard-step:not(.wizard-step-hidden)").length;
        var currentStep = $(".wizard").data("currentStep");  //If wizard initiated current step is "saved" in .wizard data-currentStep
        var $steps = $wizard.find(".wizard-step:not(.wizard-step-hidden)");
        var buttonClicked = false;
        //Default options
        var defaultOpts = {
            nextHandler: function () { },
            onloadHandler: function () { },
            backHandler: function () { },
            wizardStepRendered: function () { },
            stepText: "Step {0} of {1}",
            shiftAnimationHide: function (stepToHide) { stepToHide.hide(); },
            shiftAnimationShow: function (stepToShow) { stepToShow.show(); },
            breadcrumbDivider: " | ",
            allowMultipleClick: false,
            focusFirst: true
        };

        if (opts !== undefined) {
            defaultOpts = $.extend(defaultOpts,
                opts);
        }
        //Function to go back one step
        var goWizardBack = function () {
            currentStep = $(".wizard").data("currentStep");
            var fromPage = $steps.eq(currentStep);
            var nextPage = $steps.eq(currentStep - 1);
            if (defaultOpts.backHandler !== null) {
                var result = defaultOpts.backHandler({
                    LeavingPage: fromPage,
                    ArrivingPage: nextPage
                });
                if (result === false) {
                    return;
                }
            }
            defaultOpts.shiftAnimationHide($steps.eq(currentStep--));
            defaultOpts.shiftAnimationShow($steps.eq(currentStep));

            $(".wizard").data("currentStep", currentStep);
            wizardStepRendered({
                LastPage: fromPage,
                RenderedPage: nextPage
            });
        };
        //Function to go to one specified step. Either by sending an id as an argument or a step number
        var goWizardStep = function (idOrStep) {
            currentStep = $(".wizard").data("currentStep");
            var fromPage = $steps.eq(currentStep);
            defaultOpts.shiftAnimationHide(fromPage);

            var nextPage;
            if (typeof idOrStep === "number") {
                if (idOrStep > numberOfSteps || idOrStep < 0) {
                    throw "There are not " + idOrStep + " numbers of steps in this wizard";
                }
                nextPage = $steps.eq(idOrStep);
                defaultOpts.shiftAnimationShow(nextPage);
                currentStep = idOrStep;
            } else if (typeof idOrStep === "string" && idOrStep.charAt(0) === "#") {
                if ($(idOrStep).length === 0) {
                    throw "There is no wizard-step with id " + idOrStep;
                }
                nextPage = $(idOrStep);
                defaultOpts.shiftAnimationShow(nextPage);
                currentStep = 0;
                for (var i = 0; i < numberOfSteps; i++) {
                    if ($steps.eq(i).is(idOrStep)) {
                        break;
                    } else {
                        currentStep++;
                    }
                }
            } else {
                throw idOrStep + "is not recognized as an id nor a number";
            }
            $wizard.data("currentStep", currentStep);
            wizardStepRendered({
                LastPage: fromPage,
                RenderedPage: nextPage
            });
        };
        //Function to go to next step in wizard
        var goWizardNext = async function () {
            currentStep = $(".wizard").data("currentStep");
            var fromPage = $steps.eq(currentStep);
            var nextPage = $steps.eq(currentStep + 1);
            if (defaultOpts.nextHandler !== null) {
                var result = await defaultOpts.nextHandler({
                    PreviousStep: currentStep,
                    NextStep: currentStep + 1,
                    LeavingPage: fromPage,
                    ArrivingPage: nextPage,
                    IsLastPage: currentStep + 1 === numberOfSteps
                });
                if (result === false) {
                    return;
                }
            }
            if (currentStep + 1 !== numberOfSteps) {
                currentStep++;

                defaultOpts.shiftAnimationHide(fromPage);
                defaultOpts.shiftAnimationShow(nextPage);
                $wizard.data("currentStep", currentStep);
                wizardStepRendered({
                    LastPage: fromPage,
                    RenderedPage: nextPage
                });

            }

        };
        //Function to manage button text and other visual stuff
        var wizardStepRendered = function (args) {
            currentStep = $(".wizard").data("currentStep");
            $steps = $wizard.find(".wizard-step:not(.wizard-step-hidden)");
            if (currentStep === 0) {
                $wizard.find(".wizard-back-button").hide();
            } else {
                $wizard.find(".wizard-back-button").show();
            }

            var buttonText = $steps.eq(currentStep)
                .data("forward-button-text");
            if (buttonText === undefined) {
                buttonText = $(".wizard").data("forward-button-text");
            }
            var buttonStyled = false;
            $(".wizard-forward-button").find("*").each(function () {
                if ($(this).text().length > 0) {
                    $(this).text(buttonText);
                    buttonStyled = true;

                }
            });
            if (!buttonStyled) {
                $wizard.find(".wizard-forward-button").text(buttonText);
            }

            if (defaultOpts.focusFirst) {
                var first = $steps.eq(currentStep)
                    .find("select, input:not(:hidden), textarea").first();
                if (first.is("select")) {
                    first.focus();
                } else {
                    //Needed to work in Edge.
                    first.focus();
                    //Marks the text
                    first.select();
                }
            }

            var stepHeader = $steps.eq(currentStep)
                .data("step-header");
            stepHeader = stepHeader !== undefined ? " - " + stepHeader : "";

            $wizard.find(".wizard-step-label").text(defaultOpts.stepText
                .replace("{0}", (currentStep + 1).toString())
                .replace("{1}", numberOfSteps.toString()) +
                stepHeader);
            if (defaultOpts.wizardStepRendered !== undefined) {
                defaultOpts.wizardStepRendered(args);
            }
        };
        //End wizard step rendered
        if ($(".wizard").data("init") === undefined) {
            var defaultForwardButtonText = $wizard.data("forward-button-text");
            $(".wizard").data("currentStep", 0);
            $(".wizard").data("init", true);
            currentStep = 0;

            //This code handle the breadcrumbs
            var breadcrumbHolder = $wizard.find(".wizard-breadcrumb");
            var i = 0;
            $steps.each(function () {

                var breadcrumbId = 4232442663453 + i;
                $(this).attr("data-step-breadcrumb-id", breadcrumbId);
                var newButtonName = $(this).data("step-header") !== undefined
                    ? $(this).data("step-header")
                    : i + 1;

                //Should be changed to more easy read syntax
                breadcrumbHolder.append($("<div cursor='hand' id=" +
                    breadcrumbId +
                    " class='wizard-breadcrumb-button'>" +
                    newButtonName +
                    "</div>" + (i === numberOfSteps - 1 ? "" :
                 "<p>" + defaultOpts.breadcrumbDivider + "</p>")));
                i++;
            });

            $(".wizard-breadcrumb-button").on("click", function (event) {
                if (!$(this).data("notClickable")) {
                    currentStep = $(".wizard").data("currentStep");
                    var fromStep = $steps.eq(currentStep);
                    defaultOpts.shiftAnimationHide(fromStep);
                    var toStep = $("[data-step-breadcrumb-id= " + event.target.id + "]");
                    defaultOpts.shiftAnimationShow(toStep);
                    currentStep = toStep.data("step");
                    $(".wizard").data("currentStep", currentStep);
                    wizardStepRendered({
                        LastPage: fromStep,
                        RenderedPage: toStep
                    });
                }
                return false;
            });

            //End breadcrumb
            i = 0;
            $steps.each(function () {
                $(this).data("step", i).hide();
                i++;
            });
            $steps.eq(currentStep).show();


            $wizard.find(".wizard-back-button").click(function () {
                if (!buttonClicked || defaultOpts.allowMultipleClick) {
                    buttonClicked = true;
                    goWizardBack();
                    buttonClicked = false;
                }
            });

            $wizard.find(".wizard-forward-button").click(function () {
                if (!buttonClicked || defaultOpts.allowMultipleClick) {
                    buttonClicked = true;
                    goWizardNext();
                    buttonClicked = false;
                }
            });

            if (defaultOpts.onloadHandler !== null) {
                defaultOpts.onloadHandler();
            }
            wizardStepRendered({
                LastPage: $steps.eq(currentStep - 1),
                RenderedPage: $steps.eq(currentStep)
            });
        }

        return {
            goWizardNext: function () {
                return goWizardNext();
            },
            goWizardBack: function () {
                return goWizardBack();
            },
            goWizardStep: function (toStep) {
                return goWizardStep(toStep);
            }
        };
    }
});

